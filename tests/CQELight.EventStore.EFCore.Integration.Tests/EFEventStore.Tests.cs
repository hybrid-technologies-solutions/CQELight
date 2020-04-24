using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.EventStore.Attributes;
using CQELight.EventStore.EFCore.Common;
using CQELight.EventStore.EFCore.Models;
using CQELight.TestFramework;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CQELight.Tools.Extensions;
using Xunit;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.Abstractions.EventStore;
using Moq;
using Microsoft.EntityFrameworkCore;
using CQELight.EventStore.Snapshots;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CQELight.EventStore.EFCore.Integration.Tests
{
    public class EFEventStoreTests : BaseUnitTestClass
    {
        #region Ctor & members

        private static bool s_Init;

        private Mock<ISnapshotBehaviorProvider> _snapshotProviderMock;
        public EFEventStoreTests()
        {
            _snapshotProviderMock = new Mock<ISnapshotBehaviorProvider>();
            if (!s_Init)
            {
                using (var ctx = GetContext())
                {
                    ctx.Database.EnsureDeleted();
                    ctx.Database.EnsureCreated();
                }
                using (var aCtx = GetArchiveContext())
                {
                    aCtx.Database.EnsureDeleted();
                    aCtx.Database.EnsureCreated();
                }
                s_Init = true;
            }
            DeleteAll();
            EventStoreManager.Deactivate();
        }

        private EFEventStoreOptions GetOptions(
            BufferInfo bufferInfo = null,
            SnapshotEventsArchiveBehavior behavior = SnapshotEventsArchiveBehavior.Disabled)
        {
            return new EFEventStoreOptions(
                o => o.UseSqlite("Filename=Events_Test_Base.db"),
                _snapshotProviderMock.Object,
                bufferInfo,
                behavior,
                o => o.UseSqlite("Filename=Events_Test_Archive_Base.db"));
        }

        private DbContextOptions<EventStoreDbContext> GetBaseDbContextOptions()
            => new DbContextOptionsBuilder<EventStoreDbContext>()
                    .UseSqlite("Filename=Events_Test_Base.db")
                    .Options;
        private DbContextOptions<ArchiveEventStoreDbContext> GetArchiveDbContextOptions()
            => new DbContextOptionsBuilder<ArchiveEventStoreDbContext>()
                    .UseSqlite("Filename=Events_Test_Archive_Base.db")
                    .Options;

        private EventStoreDbContext GetContext()
            => new EventStoreDbContext(GetBaseDbContextOptions());

        private ArchiveEventStoreDbContext GetArchiveContext()
            => new ArchiveEventStoreDbContext(GetArchiveDbContextOptions());

        private void DeleteAll()
        {
            using (var ctx = GetContext())
            {
                ctx.RemoveRange(ctx.Set<Event>());
                ctx.RemoveRange(ctx.Set<Snapshot>());
                ctx.SaveChanges();
            }
            using (var ctx = GetArchiveContext())
            {
                ctx.RemoveRange(ctx.Set<ArchiveEvent>());
                ctx.SaveChanges();
            }
        }

        private Task StoreTestEventAsync(Guid aggId, Guid id, DateTime date)
            => new EFEventStore(GetOptions()).StoreDomainEventAsync(new SampleEvent(aggId, id, date)
            {
                Data = "testData"
            });

        #region Nested class

        private class SampleEvent : BaseDomainEvent
        {
            public string Data { get; set; }

            public SampleEvent(Guid aggId, Guid evtId, DateTime date)
            {
                AggregateId = aggId;
                AggregateType = typeof(AggA);
                Id = evtId;
                EventTime = date;
            }
        }

        private class SampleAggEvent : BaseDomainEvent
        {
            public string Data { get; set; }

            public SampleAggEvent(Guid aggId, Guid evtId, DateTime date)
            {
                AggregateType = typeof(SampleAgg);
                AggregateId = aggId;
                Id = evtId;
                EventTime = date;
            }
        }

        public class SampleAgg : AggregateRoot<Guid>
        {
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

        #endregion

        #endregion

        #region StoreDomainEventAsync

        [EventNotPersisted]
        private class NotPersistedEvent : BaseDomainEvent
        {

        }

        [Fact]
        public async Task StoreDomainEventAsync_NotPersistedEvent_Should_NotBePersisted()
        {
            try
            {
                DeleteAll();
                await new EFEventStore(GetOptions()).StoreDomainEventAsync(new NotPersistedEvent());

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task StoreDomainEventAsync_SimpleEvent_Should_Be_StoredInDatabase()
        {
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                Guid id = Guid.NewGuid();
                await StoreTestEventAsync(aggId, id, DateTime.Today);

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count(e => e.HashedAggregateId == aggId.ToJson().GetHashCode()).Should().Be(1);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Id.Should().Be(id);
                    evt.EventTime.Should().BeSameDateAs(DateTime.Today);
                    evt.Sequence.Should().Be(1);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task StoreDomainEventAsync_Multiples_WithoutBuffer_Should_BeStoredInDatabase()
        {
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid(); ;
                var store = new EFEventStore(GetOptions());
                for (int i = 0; i < 20; i++)
                {
                    await store.StoreDomainEventAsync(new SampleEvent(aggId, Guid.NewGuid(), DateTime.Today)
                    {
                        Data = "testData" + i
                    });
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count(e => e.HashedAggregateId == aggId.ToJson().GetHashCode()).Should().Be(20);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.EventTime.Should().BeSameDateAs(DateTime.Today);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task StoreDomainEventAsync_Multiples_WithBuffer_Should_BeStoredInDatabase()
        {
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(
                    GetOptions(new BufferInfo(new TimeSpan(0, 0, 2), new TimeSpan(0, 0, 2))));
                for (int i = 0; i < 100; i++)
                {
                    await store.StoreDomainEventAsync(new SampleEvent(aggId, Guid.NewGuid(), DateTime.Today)
                    {
                        Data = "testData" + i
                    });
                }

                await Task.Delay(2500);

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().AsNoTracking().Count(e => e.HashedAggregateId == aggId.ToJson().GetHashCode()).Should().Be(100);
                    var evt = ctx.Set<Event>().AsNoTracking().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.EventTime.Should().BeSameDateAs(DateTime.Today);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();
                    await ctx.Set<Event>().AsNoTracking().AllAsync(e => e.EventData.Contains("testData" + (e.Sequence - 1)));
                }
            }
            finally
            {
                DeleteAll();
            }
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
                var store = new EFEventStore(
                    GetOptions(new BufferInfo(new TimeSpan(0, 0, 2), new TimeSpan(0, 0, 2))));
                List<IDomainEvent> events = new List<IDomainEvent>();
                for (int i = 0; i < 100; i++)
                {
                    events.Add(new SampleEvent(aggId, Guid.NewGuid(), DateTime.Today)
                    {
                        Data = "testData" + i
                    });
                }

                await store.StoreDomainEventRangeAsync(events);

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().AsNoTracking().Count(e => e.HashedAggregateId == aggId.ToJson().GetHashCode()).Should().Be(100);
                    var evt = ctx.Set<Event>().AsNoTracking().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.EventTime.Should().BeSameDateAs(DateTime.Today);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();
                    await ctx.Set<Event>().AsNoTracking().AllAsync(e => e.EventData.Contains("testData" + (e.Sequence - 1)));
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region GetAllEventsByAggregateId

        [Fact]
        public async Task GetAllEventsByAggregateId_AsExpected()
        {
            try
            {
                DeleteAll();
                new Bootstrapper()
                    .UseEFCoreAsEventStore(
                    new EFEventStoreOptions(c =>
                        c.UseSqlite("Filename=Events_Test_Base.db")))
                    .Bootstrapp();
                var agg = new SampleAgg();
                agg.SimulateWork();
                await agg.PublishDomainEventsAsync();

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Should().HaveCount(2);
                }

                var collection = await new EFEventStore(GetOptions()).GetAllEventsByAggregateId<SampleAgg, Guid>(agg.Id).ToListAsync();
                collection.Should().HaveCount(2);

                collection.Any(e => e.GetType() == typeof(AggCreated)).Should().BeTrue();
                collection.Any(e => e.GetType() == typeof(AggDeleted)).Should().BeTrue();
                collection.All(e => e.AggregateId is Guid guidId && guidId == agg.Id).Should().BeTrue();
                collection[0].Should().BeOfType<AggCreated>();
                collection.Skip(1).First().Should().BeOfType<AggDeleted>();
            }
            finally
            {
                EventStoreManager.Deactivate();
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
                var store = new EFEventStore(GetOptions());
                List<IDomainEvent> events = new List<IDomainEvent>();
                var aggAId = Guid.NewGuid();
                var aggBId = Guid.NewGuid();
                for (int i = 0; i < 100; i++)
                {
                    if (i % 2 == 0)
                    {
                        events.Add(new EventAggA(aggAId));
                    }
                    else
                    {
                        events.Add(new EventAggB(aggBId));
                    }
                }

                await store.StoreDomainEventRangeAsync(events);

                var store2 = new EFEventStore(GetOptions());

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
                var store = new EFEventStore(GetOptions());
                List<IDomainEvent> events = new List<IDomainEvent>();
                var aggAId = Guid.NewGuid();
                var aggBId = Guid.NewGuid();
                for (int i = 0; i < 100; i++)
                {
                    if (i % 5 == 0)
                    {
                        events.Add(new EventAggA(aggAId));
                    }
                    else
                    {
                        events.Add(new EventAggB(aggBId));
                    }
                }

                await store.StoreDomainEventRangeAsync(events);

                var store2 = new EFEventStore(GetOptions());

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
                var store = new EFEventStore(GetOptions());
                List<IDomainEvent> events = new List<IDomainEvent>();
                var aggAId = Guid.NewGuid();
                var aggBId = Guid.NewGuid();
                for (int i = 0; i < 100; i++)
                {
                    if (i % 10 == 0)
                    {
                        events.Add(new EventAggA(aggAId));
                    }
                    else
                    {
                        events.Add(new EventAggB(aggBId));
                    }
                }

                await store.StoreDomainEventRangeAsync(events);

                var store2 = new EFEventStore(GetOptions());

                (await store2.GetAllEventsByEventType<EventAggA>().ToListAsync()).Should().HaveCount(10);
                (await store2.GetAllEventsByEventType<EventAggB>().ToListAsync()).Should().HaveCount(90);
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region Snapshot behavior - Generic

        private class AggregateSnapshotEvent : BaseDomainEvent
        {
            public AggregateSnapshotEvent(Guid aggregateId)
            {
                AggregateId = aggregateId;
                AggregateType = typeof(AggregateSnapshot);
                Id = Guid.NewGuid();
                EventTime = DateTime.Now;
            }
        }

        private class AggregateSnapshot : AggregateRoot<Guid>, IEventSourcedAggregate
        {
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

            public void RehydrateState(IEnumerable<IDomainEvent> events)
                => _state.ApplyRange(events);
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_Generic_Shouldnt_Be_Used_If_Set_To_Disabled()
        {
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.Disabled));
                for (int i = 0; i < 11; i++)
                {
                    var evt = new AggregateSnapshotEvent(aggId);
                    await store.StoreDomainEventAsync(evt);
                }


                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(11);
                    var evt = ctx.Set<Event>().ToList().OrderByDescending(e => e.EventTime).FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(11);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();

                    ctx.Set<Snapshot>().Count().Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_Should_RemoveEvent_If_DeleteOptions_IsChoosen()
        {
            int i = 0;
            _snapshotProviderMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent))).Returns(new NumericSnapshotBehavior(10));

            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.Delete));
                var events = new List<IDomainEvent>();
                for (i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId));
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(1);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(11);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_StoreDomainEventAsync_CreateSnapshot()
        {
            _snapshotProviderMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));

            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.Delete));
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId));
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(1);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(11);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();

                    ctx.Set<Snapshot>().Count().Should().Be(1);
                    var snap = ctx.Set<Snapshot>().FirstOrDefault();
                    snap.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                    var agg = await new EFEventStore(GetOptions()).GetRehydratedAggregateAsync<AggregateSnapshot>(aggId);
                    agg.Should().NotBeNull();
                    agg.AggIncValue.Should().Be(11);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_StoreDomainEventAsync_CreateSnapshot_Second_Should_Erase_FirstSnapshot()
        {
            _snapshotProviderMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.StoreToNewTable));
                for (int i = 0; i < 21; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId));
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(1);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(21);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();

                    ctx.Set<Snapshot>().Count().Should().Be(1);
                    var snap = ctx.Set<Snapshot>().FirstOrDefault();
                    snap.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                    var agg = await new EFEventStore(GetOptions()).GetRehydratedAggregateAsync<AggregateSnapshot>(aggId);
                    agg.Should().NotBeNull();
                    agg.AggIncValue.Should().Be(21);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_StoreDomainEventAsync_CreateSnapshot_Archive_In_DifferentTable()
        {
            _snapshotProviderMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.StoreToNewTable));

                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId));
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().ToList().Where(c => c.HashedAggregateId == aggId.ToJson(true).GetHashCode()).Count().Should().Be(1);
                    ctx.Set<ArchiveEvent>().ToList().Where(c => c.HashedAggregateId == aggId.ToJson(true).GetHashCode()).Count().Should().Be(10);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(11);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();

                    ctx.Set<Snapshot>().Count().Should().Be(1);
                    var snap = ctx.Set<Snapshot>().FirstOrDefault();
                    snap.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                    var agg = await new EFEventStore(GetOptions()).GetRehydratedAggregateAsync<AggregateSnapshot>(aggId);
                    agg.Should().NotBeNull();
                    agg.AggIncValue.Should().Be(11);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_StoreDomainEventAsync_CreateSnapshot_Archive_In_DifferentDatabase()
        {
            _snapshotProviderMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.StoreToNewDatabase));
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId));
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(1);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(11);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();

                    ctx.Set<Snapshot>().Count().Should().Be(1);
                    var snap = ctx.Set<Snapshot>().FirstOrDefault();
                    snap.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                    var agg = await new EFEventStore(GetOptions()).GetRehydratedAggregateAsync<AggregateSnapshot>(aggId);
                    agg.Should().NotBeNull();
                    agg.AggIncValue.Should().Be(11);
                }

                using (var aCtx = GetArchiveContext())
                {
                    aCtx.Set<ArchiveEvent>().Count().Should().Be(10);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_StoreDomainEventAsync_CreateSnapshot_Multiple_Same_Aggregates()
        {
            _snapshotProviderMock.Setup(m => m.GetBehaviorForEventType(typeof(AggregateSnapshotEvent)))
                .Returns(new NumericSnapshotBehavior(10));
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.Delete));
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId));
                }
                var otherId = Guid.NewGuid();
                for (int i = 0; i < 30; i++)
                {
                    if (i % 10 == 0)
                    {
                        otherId = Guid.NewGuid();
                    }
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(otherId));
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count(e => e.HashedAggregateId == aggId.ToJson(true).GetHashCode()).Should().Be(1);
                    var evt = ctx.Set<Event>().ToList().Where(e => e.HashedAggregateId == aggId.ToJson(true).GetHashCode()).FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(11);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();

                    ctx.Set<Snapshot>().Count().Should().Be(1);
                    var snap = ctx.Set<Snapshot>().FirstOrDefault();
                    snap.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    snap.AggregateType.Should().Be(typeof(AggregateSnapshot).AssemblyQualifiedName);

                    var agg = await new EFEventStore(GetOptions()).GetRehydratedAggregateAsync<AggregateSnapshot>(aggId);
                    agg.Should().NotBeNull();
                    agg.AggIncValue.Should().Be(11);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task SnapshotBehavior_Generic_StoreDomainEventAsync_NoSnapshotBehaviorDefined()
        {
            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions());
                for (int i = 0; i < 11; i++)
                {
                    await store.StoreDomainEventAsync(new AggregateSnapshotEvent(aggId));
                }

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(11);

                    ctx.Set<Snapshot>().Count().Should().Be(0);

                    var agg = await new EFEventStore(GetOptions()).GetRehydratedAggregateAsync<AggregateSnapshot>(aggId);
                    agg.Should().NotBeNull();
                    agg.AggIncValue.Should().Be(11);
                }
            }
            finally
            {
                DeleteAll();
            }
        }
        #endregion

        #region Snapshot behavior - Specific

        class FirstEvent : BaseDomainEvent
        {
            public FirstEvent(Guid aggId)
            {
                AggregateId = aggId;
                AggregateType = typeof(BusinessAggregate);
            }
        }
        class SecondEvent : BaseDomainEvent
        {
            public SecondEvent(Guid aggId)
            {
                AggregateId = aggId;
                AggregateType = typeof(BusinessAggregate);
            }
        }
        class ThirdEvent : BaseDomainEvent
        {
            public ThirdEvent(Guid aggId)
            {
                AggregateId = aggId;
                AggregateType = typeof(BusinessAggregate);
            }
        }

        class BusinessState : AggregateState
        {
            public int CurrentState;
            public BusinessState()
            {
                AddHandler<FirstEvent>(OnFirst);
                AddHandler<SecondEvent>(OnSecond);
                AddHandler<ThirdEvent>(OnThird);
            }

            private void OnThird(ThirdEvent obj) { CurrentState = 3; }

            private void OnSecond(SecondEvent obj) { CurrentState = 2; }

            private void OnFirst(FirstEvent obj) { CurrentState = 1; }
        }
        class BusinessAggregate : EventSourcedAggregate<Guid, BusinessState>
        {
            public int CurrentState => State.CurrentState;
            public BusinessAggregate()
            {
                State = new BusinessState();
            }
        }

        class SpecificSnapshotBehavior : ISnapshotBehavior
        {
            public IEnumerable<IDomainEvent> GenerateSnapshot(AggregateState rehydratedAggregateState)
            {
                var newState = rehydratedAggregateState.GetType().CreateInstance() as AggregateState;
                var events = rehydratedAggregateState.Events.Where(e => !(e is ThirdEvent));

                newState.ApplyRange(events);
                return events;
            }

            public bool IsSnapshotNeeded(IDomainEvent @event)
                => @event is ThirdEvent;
        }

        [Fact]
        public async Task Snapshot_Behavior_Specific_Should_Respect_Rules()
        {
            _snapshotProviderMock.Setup(m => m.GetBehaviorForEventType(typeof(ThirdEvent)))
                           .Returns(new SpecificSnapshotBehavior());

            try
            {
                DeleteAll();
                Guid aggId = Guid.NewGuid();
                var store = new EFEventStore(GetOptions(behavior: SnapshotEventsArchiveBehavior.Delete));
                await store.StoreDomainEventRangeAsync(new IDomainEvent[]
                {
                    new FirstEvent(aggId),
                    new SecondEvent(aggId),
                    new ThirdEvent(aggId),
                });

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count().Should().Be(1);
                    var evt = ctx.Set<Event>().FirstOrDefault();
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Sequence.Should().Be(3);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();
                    evt.EventType.Should().Be(typeof(ThirdEvent).AssemblyQualifiedName);

                    ctx.Set<Snapshot>().Count().Should().Be(1);
                    var snap = ctx.Set<Snapshot>().FirstOrDefault();
                    snap.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    snap.AggregateType.Should().Be(typeof(BusinessAggregate).AssemblyQualifiedName);

                    var agg = await new EFEventStore(GetOptions()).GetRehydratedAggregateAsync<BusinessAggregate>(aggId);
                    agg.Should().NotBeNull();
                    agg.CurrentState.Should().Be(3);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region ComputeSequence

        [Fact]
        public async Task Sequence_Should_Consider_AggregateId_And_AggregateType()
        {
            try
            {
                DeleteAll();
                var eventStore = new EFEventStore(GetOptions());
                Guid aggId = Guid.NewGuid();
                var evt1Id = Guid.NewGuid();
                var evt2Id = Guid.NewGuid();
                await eventStore.StoreDomainEventAsync(new SampleEvent(aggId, evt1Id, DateTime.Today)
                {
                    Data = "testData"
                });
                await eventStore.StoreDomainEventAsync(new SampleAggEvent(aggId, evt2Id, DateTime.Today)
                {
                    Data = "testData"
                });

                using (var ctx = GetContext())
                {
                    ctx.Set<Event>().Count(e => e.HashedAggregateId == aggId.ToJson().GetHashCode()).Should().Be(2);
                    var evt = ctx.Set<Event>().First(e => e.AggregateType == typeof(AggA).AssemblyQualifiedName);
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Id.Should().Be(evt1Id);
                    evt.EventTime.Should().BeSameDateAs(DateTime.Today);
                    evt.Sequence.Should().Be(1);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();

                    evt = ctx.Set<Event>().First(e => e.AggregateType == typeof(SampleAgg).AssemblyQualifiedName);
                    evt.Should().NotBeNull();
                    evt.HashedAggregateId.Should().Be(aggId.ToJson(true).GetHashCode());
                    evt.Id.Should().Be(evt2Id);
                    evt.EventTime.Should().BeSameDateAs(DateTime.Today);
                    evt.Sequence.Should().Be(1);
                    evt.EventData.Should().NotBeNullOrWhiteSpace();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

    }
}
