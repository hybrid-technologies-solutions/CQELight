using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore.Attributes;
using CQELight.EventStore.MongoDb.Models;
using CQELight.EventStore.Snapshots;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.EventStore.MongoDb.Integration.Tests
{
    public class MongoDbEventStoreTests : BaseUnitTestClass
    {
        #region Ctor & members

        private static bool s_Init;
        private Mock<ISnapshotBehaviorProvider> _snapshotBehaviorMock;

        public MongoDbEventStoreTests()
        {
            _snapshotBehaviorMock = new Mock<ISnapshotBehaviorProvider>();
            if (!s_Init)
            {
                var c = new ConfigurationBuilder().AddJsonFile("test-config.json").Build();
                new Bootstrapper()
                    .UseMongoDbAsEventStore(new MongoEventStoreOptions(
                        c["user"], c["password"],
                        _snapshotBehaviorMock.Object,
                        serversUrls: $"{c["host"]}:{c["port"]}"))
                    .Bootstrapp();
                s_Init = true;
            }
            EventStoreManager.Options.ShouldPersistNonAggregateEvent = true;
            DeleteAll();
        }

        private void DeleteAll()
        {
            EventStoreManager.Client.DropDatabase(Consts.CONST_DB_NAME);
            EventStoreManager.Client.DropDatabase(Consts.CONST_ARCHIVE_DB_NAME);
        }

        private IMongoCollection<IDomainEvent> GetEventCollection()
            => EventStoreManager.Client.GetDatabase(Consts.CONST_DB_NAME).GetCollection<IDomainEvent>(Consts.CONST_EVENTS_COLLECTION_NAME);
        private IMongoCollection<IDomainEvent> GetEventArchiveCollection()
            => EventStoreManager.Client.GetDatabase(Consts.CONST_DB_NAME).GetCollection<IDomainEvent>(Consts.CONST_ARCHIVE_EVENTS_COLLECTION_NAME);
        private IMongoCollection<Snapshot> GetSnapshotCollection()
            => EventStoreManager.Client.GetDatabase(Consts.CONST_DB_NAME).GetCollection<Snapshot>(Consts.CONST_SNAPSHOT_COLLECTION_NAME);
        private IMongoCollection<IDomainEvent> GetEventsArchiveOtherDb()
            => EventStoreManager.Client.GetDatabase(Consts.CONST_ARCHIVE_DB_NAME).GetCollection<IDomainEvent>(Consts.CONST_ARCHIVE_EVENTS_COLLECTION_NAME);

        private async Task StoreTestEventAsync(Guid aggId, Guid id, DateTime date)
        {
            var store = new MongoDbEventStore();
            await store.StoreDomainEventAsync(new SampleEvent(aggId, id, date)
            {
                Data = "testData"
            }).ConfigureAwait(false);
        }

        #region Nested class

        [EventNotPersisted]
        private class NotPersistedEvent : BaseDomainEvent
        {

        }

        private class SampleEvent : BaseDomainEvent
        {
            public string Data { get; set; }

            public SampleEvent(Guid aggId, Guid evtId, DateTime date)
            {
                AggregateId = aggId;
                Id = evtId;
                EventTime = date;
            }
        }

        public class SampleAgg : AggregateRoot<Guid>
        {
            public SampleAgg()
            {
                Id = Guid.NewGuid();
            }
            public void SimulateWork()
            {
                AddDomainEvent(new AggCreated());
                AddDomainEvent(new AggDeleted());
            }
        }

        public class AggCreated : BaseDomainEvent
        {
        }
        public class AggDeleted : BaseDomainEvent
        {
        }

        public class NonAggEvent : BaseDomainEvent
        {
        }

        #endregion

        #endregion

        #region StoreDomainEventAsync

        [Fact]
        public async Task MongoDbEventStoreStoreDomainEventAsync_NotPersisted()
        {
            try
            {
                var store = new MongoDbEventStore();
                await store.StoreDomainEventAsync(new NotPersistedEvent()).ConfigureAwait(false);
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(0);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStoreStoreDomainEventAsync_AsExpected()
        {
            try
            {
                Guid aggId = Guid.NewGuid();
                Guid id = Guid.NewGuid();
                DateTime date = new DateTime(2018, 1, 1, 12, 00, 01);
                await StoreTestEventAsync(aggId, id, date).ConfigureAwait(false);

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(1);
                var evt = await GetEventCollection().Find(FilterDefinition<IDomainEvent>.Empty).FirstOrDefaultAsync().ConfigureAwait(false);
                evt.Should().NotBeNull();
                evt.AggregateId.Should().Be(aggId);
                evt.Id.Should().Be(id);
                evt.EventTime.Should().BeSameDateAs(date);
                evt.Sequence.Should().Be(1);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task NonAggregateEvent_Should_Be_Persisted_If_User_Configure_It()
        {
            try
            {
                EventStoreManager.Options.ShouldPersistNonAggregateEvent = true;
                DeleteAll();
                var store = new MongoDbEventStore();
                await store.StoreDomainEventAsync(new NonAggEvent());

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(1);
                var evt = await (await GetEventCollection().FindAsync(FilterDefinition<IDomainEvent>.Empty)).FirstOrDefaultAsync();
                evt.Should().NotBeNull();

                evt.AggregateType.Should().BeNull();
                evt.AggregateId.Should().BeNull();
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task NonAggregateEvent_Shouldnt_Be_Persisted_If_User_Configure_It()
        {
            EventStoreManager.Options.ShouldPersistNonAggregateEvent = false;
            DeleteAll();
            var store = new MongoDbEventStore();
            await store.StoreDomainEventAsync(new NonAggEvent());

            (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(0);
        }

        #endregion

        #region StoreDomainEventRangeAsync

        [Fact]
        public async Task StoreDomainEventRangeAsync_Should_Store_AllEvents_ToDb()
        {
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new MongoDbEventStore();
                List<IDomainEvent> events = new List<IDomainEvent>();
                for (int i = 0; i < 100; i++)
                {
                    events.Add(new SampleEvent(aggId, Guid.NewGuid(), DateTime.Today)
                    {
                        Data = "testData" + i
                    });
                }

                await store.StoreDomainEventRangeAsync(events);

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(100);
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region GetAllEventsByAggregateType

        public class AggA : AggregateRoot<Guid> { }
        public class AggB : AggregateRoot<Guid> { }
        public class EventAggA : BaseDomainEvent
        {
            public EventAggA(Guid id)
            {
                AggregateId = id;
                AggregateType = typeof(AggA);
            }
        }

        public class EventAggB : BaseDomainEvent
        {
            public EventAggB(Guid id)
            {
                AggregateId = id;
                AggregateType = typeof(AggB);
            }
        }

        [Fact]
        public async Task GetAllEventsByAggregateType_Should_Returns_AllConcernedEvents()
        {
            try
            {
                DeleteAll();
                var store = new MongoDbEventStore();
                List<IDomainEvent> events = new List<IDomainEvent>();
                for (int i = 0; i < 100; i++)
                {
                    if (i % 2 == 0)
                    {
                        events.Add(new EventAggA(Guid.NewGuid()));
                    }
                    else
                    {
                        events.Add(new EventAggB(Guid.NewGuid()));
                    }
                }

                await store.StoreDomainEventRangeAsync(events);

                var store2 = new MongoDbEventStore();

                (await store2.GetAllEventsByAggregateType(typeof(AggA)).ToListAsync()).Should().HaveCount(50);
                (await store2.GetAllEventsByAggregateType(typeof(AggB)).ToListAsync()).Should().HaveCount(50);
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region GetAllEventsByEventType

        [Fact]
        public async Task GetAllEventsByEventType_Should_Returns_OnlyConcernedEvents()
        {
            try
            {
                DeleteAll();
                var store = new MongoDbEventStore();
                List<IDomainEvent> events = new List<IDomainEvent>();
                for (int i = 0; i < 100; i++)
                {
                    if (i % 5 == 0)
                    {
                        events.Add(new EventAggA(Guid.NewGuid()));
                    }
                    else
                    {
                        events.Add(new EventAggB(Guid.NewGuid()));
                    }
                }

                await store.StoreDomainEventRangeAsync(events);

                var store2 = new MongoDbEventStore();

                (await store2.GetAllEventsByEventType(typeof(EventAggA)).ToListAsync()).Should().HaveCount(20);
                (await store2.GetAllEventsByEventType(typeof(EventAggB)).ToListAsync()).Should().HaveCount(80);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task GetAllEventsByEventType_Generic_Should_Returns_OnlyConcernedEvents()
        {
            try
            {
                DeleteAll();
                var store = new MongoDbEventStore();
                List<IDomainEvent> events = new List<IDomainEvent>();
                for (int i = 0; i < 100; i++)
                {
                    if (i % 10 == 0)
                    {
                        events.Add(new EventAggA(Guid.NewGuid()));
                    }
                    else
                    {
                        events.Add(new EventAggB(Guid.NewGuid()));
                    }
                }

                await store.StoreDomainEventRangeAsync(events);

                var store2 = new MongoDbEventStore();

                (await store2.GetAllEventsByEventType<EventAggA>().ToListAsync()).Should().HaveCount(10);
                (await store2.GetAllEventsByEventType<EventAggB>().ToListAsync()).Should().HaveCount(90);
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region Snapshot behavior

        private class AggregateSnapshotEvent : BaseDomainEvent
        {
            public AggregateSnapshotEvent(Guid aggregateId)
            {
                AggregateId = aggregateId;
                AggregateType = typeof(AggregateSnapshot);
                Id = Guid.NewGuid();
            }
        }

        private class AggregateSnapshot : EventSourcedAggregate<Guid, AggregateState>
        {
            protected override AggregateState State
            {
                get => _state;
                set
                {
                    if (value is AggregateSnapshotState newState)
                    {
                        _state = newState;
                    }
                }
            }
            private AggregateSnapshotState _state = new AggregateSnapshotState();
            public int AggIncValue => _state.Increment;

            private class AggregateSnapshotState : AggregateState
            {
                public int Increment { get; private set; }

                public AggregateSnapshotState()
                {
                    AddHandler<AggregateSnapshotEvent>(AggregateSnapshotEventWhen);
                }

                private void AggregateSnapshotEventWhen(AggregateSnapshotEvent obj)
                    => Increment++;
            }

            public override void RehydrateState(IEnumerable<IDomainEvent> events)
                => _state.ApplyRange(events);
        }

        [Fact]
        public async Task StoreDomainEvent_Should_Apply_RetrievedSequence()
        {
            _snapshotBehaviorMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent))).Returns(new NumericSnapshotBehavior(10));

            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();

                var store = new MongoDbEventStore(_snapshotBehaviorMock.Object);
                for (int i = 0; i < 11; i++)
                {
                    var evt = new AggregateSnapshotEvent(aggId);
                    await store.StoreDomainEventAsync(evt).ConfigureAwait(false);
                }
                (await GetEventsArchiveOtherDb().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventArchiveCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(10);
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(1);

                var allEvents = await store.GetAllEventsByAggregateId<AggregateSnapshot, Guid>(aggId).ToListAsync();
                allEvents.Should().HaveCount(1);
                var dbEvt = allEvents.FirstOrDefault().As<AggregateSnapshotEvent>();
                dbEvt.Should().NotBeNull();
                dbEvt.Should().BeOfType<AggregateSnapshotEvent>();
                dbEvt.AggregateId.Should().Be(aggId);
                dbEvt.Sequence.Should().Be(11);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStoreStoreDomainEventAsync_CreateSnapshot_ArchiveTable()
        {
            _snapshotBehaviorMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(0);
            }
            finally
            {
                DeleteAll();
            }
            try
            {
                var store = new MongoDbEventStore(_snapshotBehaviorMock.Object);
                Guid aggId = Guid.NewGuid();
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId)).ConfigureAwait(false);
                }

                (await GetEventsArchiveOtherDb().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventArchiveCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(10);
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(1);

                var evt = (await store.GetAllEventsByAggregateId<AggregateSnapshot, Guid>(aggId).FirstOrDefaultAsync()).As<AggregateSnapshotEvent>();
                evt.Should().NotBeNull();
                evt.Should().BeOfType<AggregateSnapshotEvent>();
                evt.AggregateId.Should().Be(aggId);
                evt.Sequence.Should().Be(11);

                (await GetSnapshotCollection().CountDocumentsAsync(FilterDefinition<Snapshot>.Empty)).Should().Be(1);

                var snapFilter = Builders<Snapshot>.Filter.Eq(nameof(Snapshot.AggregateId), (object)aggId);
                var snap = await (await GetSnapshotCollection().FindAsync(snapFilter)).FirstOrDefaultAsync();
                snap.Should().NotBeNull();
                snap.AggregateId.Should().Be(aggId);
                snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                store = new MongoDbEventStore();
                var agg = await store.GetRehydratedAggregateAsync<AggregateSnapshot>(aggId).ConfigureAwait(false);
                agg.Should().NotBeNull();
                agg.AggIncValue.Should().Be(11);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStoreStoreDomainEventAsync_CreateSnapshot_ArchiveTable_Creating_Two_Snapshot_Should_Only_Have_One_InDatabase()
        {
            _snapshotBehaviorMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(0);
            }
            finally
            {
                DeleteAll();
            }
            try
            {
                var store = new MongoDbEventStore(_snapshotBehaviorMock.Object);
                Guid aggId = Guid.NewGuid();
                for (int i = 0; i < 21; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId)).ConfigureAwait(false);
                }

                (await GetEventsArchiveOtherDb().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventArchiveCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(20);
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(1);

                var evt = (await store.GetAllEventsByAggregateId<AggregateSnapshot, Guid>(aggId).FirstOrDefaultAsync()).As<AggregateSnapshotEvent>();
                evt.Should().NotBeNull();
                evt.Should().BeOfType<AggregateSnapshotEvent>();
                evt.AggregateId.Should().Be(aggId);
                evt.Sequence.Should().Be(21);

                (await GetSnapshotCollection().CountDocumentsAsync(FilterDefinition<Snapshot>.Empty)).Should().Be(1);

                var snapFilter = Builders<Snapshot>.Filter.Eq(nameof(Snapshot.AggregateId), (object)aggId);
                var snap = await (await GetSnapshotCollection().FindAsync(snapFilter)).FirstOrDefaultAsync();
                snap.Should().NotBeNull();
                snap.AggregateId.Should().Be(aggId);
                snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                store = new MongoDbEventStore();
                var agg = await store.GetRehydratedAggregateAsync<AggregateSnapshot>(aggId).ConfigureAwait(false);
                agg.Should().NotBeNull();
                agg.AggIncValue.Should().Be(21);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStoreStoreDomainEventAsync_Disabled_Should_Not_TouchEvent_Nor_CreateSnapshot()
        {
            _snapshotBehaviorMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                   .Returns(new NumericSnapshotBehavior(10));
            try
            {

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(0);
            }
            finally
            {
                DeleteAll();
            }
            try
            {
                var store = new MongoDbEventStore(_snapshotBehaviorMock.Object, SnapshotEventsArchiveBehavior.Disabled);
                Guid aggId = Guid.NewGuid();
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId)).ConfigureAwait(false);
                }

                (await GetEventsArchiveOtherDb().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventArchiveCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(11);

                var evts = await store.GetAllEventsByAggregateId<AggregateSnapshot, Guid>(aggId).ToListAsync();
                evts.Should().HaveCount(11);
                var evt = evts.OrderByDescending(m => m.Sequence).FirstOrDefault();
                evt.Should().BeOfType<AggregateSnapshotEvent>();
                evt.AggregateId.Should().Be(aggId);
                evt.Sequence.Should().Be(11);

                (await GetSnapshotCollection().CountDocumentsAsync(FilterDefinition<Snapshot>.Empty)).Should().Be(0);

                store = new MongoDbEventStore();
                var agg = await store.GetRehydratedAggregateAsync<AggregateSnapshot>(aggId).ConfigureAwait(false);
                agg.Should().NotBeNull();
                agg.AggIncValue.Should().Be(11);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStoreStoreDomainEventAsync_CreateSnapshot_Deletion()
        {
            _snapshotBehaviorMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(0);
            }
            finally
            {
                DeleteAll();
            }
            try
            {
                var store = new MongoDbEventStore(_snapshotBehaviorMock.Object, SnapshotEventsArchiveBehavior.Delete);
                Guid aggId = Guid.NewGuid();
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId)).ConfigureAwait(false);
                }

                (await GetEventsArchiveOtherDb().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventArchiveCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(1);

                var evt = (await store.GetAllEventsByAggregateId<AggregateSnapshot, Guid>(aggId).FirstOrDefaultAsync()).As<AggregateSnapshotEvent>();
                evt.Should().NotBeNull();
                evt.Should().BeOfType<AggregateSnapshotEvent>();
                evt.AggregateId.Should().Be(aggId);
                evt.Sequence.Should().Be(11);

                (await GetSnapshotCollection().CountDocumentsAsync(FilterDefinition<Snapshot>.Empty)).Should().Be(1);

                var snapFilter = Builders<Snapshot>.Filter.Eq(nameof(Snapshot.AggregateId), (object)aggId);
                var snap = await (await GetSnapshotCollection().FindAsync(snapFilter)).FirstOrDefaultAsync();
                snap.Should().NotBeNull();
                snap.AggregateId.Should().Be(aggId);
                snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                store = new MongoDbEventStore();
                var agg = await store.GetRehydratedAggregateAsync<AggregateSnapshot>(aggId).ConfigureAwait(false);
                agg.Should().NotBeNull();
                agg.AggIncValue.Should().Be(11);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStoreStoreDomainEventAsync_CreateSnapshot_ToNewDatabase()
        {
            _snapshotBehaviorMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty).ConfigureAwait(false)).Should().Be(0);
            }
            finally
            {
                DeleteAll();
            }
            try
            {
                var store = new MongoDbEventStore(_snapshotBehaviorMock.Object, SnapshotEventsArchiveBehavior.StoreToNewDatabase);
                Guid aggId = Guid.NewGuid();
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId)).ConfigureAwait(false);
                }

                (await GetEventsArchiveOtherDb().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(10);
                (await GetEventArchiveCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(0);
                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(1);

                var evt = (await store.GetAllEventsByAggregateId<AggregateSnapshot, Guid>(aggId).FirstOrDefaultAsync()).As<AggregateSnapshotEvent>();
                evt.Should().NotBeNull();
                evt.Should().BeOfType<AggregateSnapshotEvent>();
                evt.AggregateId.Should().Be(aggId);
                evt.Sequence.Should().Be(11);

                (await GetSnapshotCollection().CountDocumentsAsync(FilterDefinition<Snapshot>.Empty)).Should().Be(1);

                var snapFilter = Builders<Snapshot>.Filter.Eq(nameof(Snapshot.AggregateId), (object)aggId);
                var snap = await (await GetSnapshotCollection().FindAsync(snapFilter)).FirstOrDefaultAsync();
                snap.Should().NotBeNull();
                snap.AggregateId.Should().Be(aggId);
                snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                store = new MongoDbEventStore();
                var agg = await store.GetRehydratedAggregateAsync<AggregateSnapshot>(aggId).ConfigureAwait(false);
                agg.Should().NotBeNull();
                agg.AggIncValue.Should().Be(11);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStore_StoreDomainEventAsync_CreateSnapshot_Multiple_Same_Aggregates()
        {
            _snapshotBehaviorMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new MongoDbEventStore(_snapshotBehaviorMock.Object);
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId)).ConfigureAwait(false);
                }
                var otherId = Guid.NewGuid();
                for (int i = 0; i <= 33; i++)
                {
                    if (i % 11 == 0)
                    {
                        otherId = Guid.NewGuid();
                    }
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(otherId)).ConfigureAwait(false);
                }

                var evt = (await store.GetAllEventsByAggregateId<AggregateSnapshot, Guid>(aggId).FirstOrDefaultAsync()).As<AggregateSnapshotEvent>();
                evt.Should().NotBeNull();
                evt.Should().BeOfType<AggregateSnapshotEvent>();
                evt.AggregateId.Should().Be(aggId);
                evt.Sequence.Should().Be(11);

                var snapshotFilter = Builders<Snapshot>.Filter.Eq(nameof(Snapshot.AggregateId), (object)aggId);
                (await GetSnapshotCollection().CountDocumentsAsync(snapshotFilter)).Should().Be(1);
                var snap = await (await GetSnapshotCollection().FindAsync(snapshotFilter)).FirstOrDefaultAsync();
                snap.Should().NotBeNull();
                snap.AggregateId.Should().Be(aggId);
                snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                store = new MongoDbEventStore();
                var agg = await store.GetRehydratedAggregateAsync<AggregateSnapshot>(aggId).ConfigureAwait(false);
                agg.Should().NotBeNull();
                agg.AggIncValue.Should().Be(11);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoDbEventStore_StoreDomainEventAsync_NoSnapshotBehaviorDefined()
        {
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new MongoDbEventStore();
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId)).ConfigureAwait(false);
                }

                (await GetEventCollection().CountDocumentsAsync(FilterDefinition<IDomainEvent>.Empty)).Should().Be(11);
                (await GetSnapshotCollection().CountDocumentsAsync(FilterDefinition<Snapshot>.Empty)).Should().Be(0);

                var agg = await store.GetRehydratedAggregateAsync<AggregateSnapshot>(aggId).ConfigureAwait(false);
                agg.Should().NotBeNull();
                agg.AggIncValue.Should().Be(11);
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

    }
}
