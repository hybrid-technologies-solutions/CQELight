using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore.Attributes;
using CQELight.EventStore.MongoDb.Models;
using CQELight.Tools.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CQELight.EventStore.MongoDb
{
    /// <summary>
    /// MongoDb Event store implementation.
    /// </summary>
    public class MongoDbEventStore : IEventStore, IAggregateEventStore
    {
        #region Private members

        private readonly ISnapshotBehaviorProvider? _snapshotBehaviorProvider;
        private readonly SnapshotEventsArchiveBehavior _archiveBehavior;

        #endregion

        #region Ctor

        public MongoDbEventStore(ISnapshotBehaviorProvider? snapshotBehaviorProvider = null,
            SnapshotEventsArchiveBehavior archiveBehavior = SnapshotEventsArchiveBehavior.StoreToNewTable)
        {
            _snapshotBehaviorProvider = snapshotBehaviorProvider;
            _archiveBehavior = archiveBehavior;
        }

        #endregion

        #region IEventStore

#if NETSTANDARD2_0
        public IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateType(Type aggregateType)
        {
            var filterBuilder = Builders<IDomainEvent>.Filter;
            var filter = filterBuilder.Eq(nameof(IDomainEvent.AggregateType), aggregateType);

            var collection = EventStoreManager.Client
                            .GetDatabase(Consts.CONST_DB_NAME)
                            .GetCollection<IDomainEvent>(Consts.CONST_EVENTS_COLLECTION_NAME);

            return collection.Find(filter).ToEnumerable().ToAsyncEnumerable();
        }

        public IAsyncEnumerable<T> GetAllEventsByEventType<T>()
            where T : class, IDomainEvent
            => EventStoreManager.Client
                            .GetDatabase(Consts.CONST_DB_NAME)
                            .GetCollection<T>(Consts.CONST_EVENTS_COLLECTION_NAME)
                            .Find(new BsonDocument("_t", typeof(T).Name))
                            .ToList()
                            .ToAsyncEnumerable();

        public IAsyncEnumerable<IDomainEvent> GetAllEventsByEventType(Type eventType)
            => EventStoreManager.Client
                            .GetDatabase(Consts.CONST_DB_NAME)
                            .GetCollection<IDomainEvent>(Consts.CONST_EVENTS_COLLECTION_NAME)
                            .Find(new BsonDocument("_t", eventType.Name))
                            .ToList()
                            .ToAsyncEnumerable();

        public IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId(Type aggregateType, object aggregateId)
        {
            var filterBuilder = Builders<IDomainEvent>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(nameof(IDomainEvent.AggregateId), aggregateId),
                filterBuilder.Eq(nameof(IDomainEvent.AggregateType), aggregateType));

            var collection = EventStoreManager.Client
                            .GetDatabase(Consts.CONST_DB_NAME)
                            .GetCollection<IDomainEvent>(Consts.CONST_EVENTS_COLLECTION_NAME);

            return collection.Find(filter).Sort(Builders<IDomainEvent>.Sort.Ascending(nameof(IDomainEvent.Sequence))).ToEnumerable().ToAsyncEnumerable();
        }
#elif NETSTANDARD2_1
        public async IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateType(Type aggregateType)
        {
            var filterBuilder = Builders<IDomainEvent>.Filter;
            var filter = filterBuilder.Eq(nameof(IDomainEvent.AggregateType), aggregateType);

            var collection = EventStoreManager.Client
                            .GetDatabase(Consts.CONST_DB_NAME)
                            .GetCollection<IDomainEvent>(Consts.CONST_EVENTS_COLLECTION_NAME);
            foreach (var item in await (await collection.FindAsync(filter).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<T> GetAllEventsByEventType<T>()
            where T : class, IDomainEvent
        {
            foreach (var item in await (await EventStoreManager.Client
                              .GetDatabase(Consts.CONST_DB_NAME)
                              .GetCollection<T>(Consts.CONST_EVENTS_COLLECTION_NAME)
                              .FindAsync(new BsonDocument("_t", typeof(T).Name)).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<IDomainEvent> GetAllEventsByEventType(Type eventType)
        {
            foreach (var item in await (await EventStoreManager.Client
                              .GetDatabase(Consts.CONST_DB_NAME)
                              .GetCollection<IDomainEvent>(Consts.CONST_EVENTS_COLLECTION_NAME)
                              .FindAsync(new BsonDocument("_t", eventType.Name)).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId(Type aggregateType, object aggregateId)
        {
            var filterBuilder = Builders<IDomainEvent>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(nameof(IDomainEvent.AggregateId), aggregateId),
                filterBuilder.Eq(nameof(IDomainEvent.AggregateType), aggregateType));

            var collection = EventStoreManager.Client
                            .GetDatabase(Consts.CONST_DB_NAME)
                            .GetCollection<IDomainEvent>(Consts.CONST_EVENTS_COLLECTION_NAME);

            foreach (var item in await collection.Find(filter).Sort(Builders<IDomainEvent>.Sort.Ascending(nameof(IDomainEvent.Sequence))).ToListAsync().ConfigureAwait(false))
            {
                yield return item;
            }
        }
#endif

        public IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId<TAggregateType, TAggregateId>(TAggregateId id)
            where TAggregateType : AggregateRoot<TAggregateId>
            where TAggregateId : notnull
        => GetAllEventsByAggregateId(typeof(TAggregateType), id);

        public async Task<Result> StoreDomainEventRangeAsync(IEnumerable<IDomainEvent> events)
        {
            foreach (var evt in events)
            {
                await PrepareForEventInsertionAsync(evt).ConfigureAwait(false);
            }

            var collection = await GetEventCollectionAsync<IDomainEvent>().ConfigureAwait(false);
            await collection.InsertManyAsync(events).ConfigureAwait(false);
            return Result.Ok();
        }

        public async Task<Result> StoreDomainEventAsync(IDomainEvent @event)
        {
            if (@event.GetType().IsDefined(typeof(EventNotPersistedAttribute)))
            {
                return Result.Ok();
            }
            await PrepareForEventInsertionAsync(@event).ConfigureAwait(false);

            var collection = await GetEventCollectionAsync<IDomainEvent>().ConfigureAwait(false);
            await collection.InsertOneAsync(@event).ConfigureAwait(false);
            return Result.Ok();
        }

        #endregion

        #region IAggregateEventStore

        public async Task<IEventSourcedAggregate> GetRehydratedAggregateAsync(object aggregateUniqueId, Type aggregateType)
        {
            if (aggregateType == null)
            {
                throw new ArgumentNullException(nameof(aggregateType));
            }

            if (!(aggregateType.CreateInstance() is IEventSourcedAggregate aggInstance))
            {
                throw new InvalidOperationException("MongoDbEventStore.GetRehydratedAggregateAsync() : Cannot create a new instance of" +
                    $" {aggregateType.FullName} aggregate. It should have one parameterless constructor (can be private).");
            }

            var state = await GetRehydratedStateAsync(aggregateUniqueId, aggregateType).ConfigureAwait(false);

            PropertyInfo? stateProp = aggregateType.GetAllProperties().FirstOrDefault(p => p.PropertyType.IsSubclassOf(typeof(AggregateState)));
            FieldInfo? stateField = aggregateType.GetAllFields().FirstOrDefault(f => f.FieldType.IsSubclassOf(typeof(AggregateState)));
            Type? stateType = stateProp?.PropertyType ?? stateField?.FieldType;
            if (stateType != null)
            {
                if (stateProp != null)
                {
                    stateProp.SetValue(aggInstance, state);
                }
                else
                {
                    stateField!.SetValue(aggInstance, state);
                }
            }
            else
            {
                throw new InvalidOperationException("MongoDbEventStore.GetRehydratedAggregateAsync() : Cannot find property/field that manage state for aggregate" +
                    $" type {aggregateType.FullName}. State should be a property or a field of the aggregate");
            }

            return aggInstance;
        }

        public async Task<TAggregate> GetRehydratedAggregateAsync<TAggregate>(object aggregateUniqueId)
            where TAggregate : class, IEventSourcedAggregate
            => (TAggregate)(await GetRehydratedAggregateAsync(aggregateUniqueId, typeof(TAggregate)).ConfigureAwait(false));

        #endregion

        #region Private methods

        private async Task<AggregateState> GetRehydratedStateAsync<TId>(TId aggregateUniqueId, Type aggregateType)
            where TId : notnull
        {
            var snapshot = await GetSnapshotFromAggregateId(aggregateUniqueId, aggregateType).ConfigureAwait(false);

            PropertyInfo? stateProp = aggregateType.GetAllProperties().FirstOrDefault(p => p.PropertyType.IsSubclassOf(typeof(AggregateState)));
            FieldInfo? stateField = aggregateType.GetAllFields().FirstOrDefault(f => f.FieldType.IsSubclassOf(typeof(AggregateState)));
            Type? stateType = stateProp?.PropertyType ?? stateField?.FieldType;
            AggregateState? state = null;
            if (stateType != null)
            {
                if (snapshot != null)
                {
                    state = snapshot.AggregateState;
                }
                else
                {
                    state = stateType.CreateInstance() as AggregateState;
                    if (state == null)
                    {
                        throw new InvalidOperationException($"MongoDbEventStore.GetRehydratedAggregateAsync() : Unable to create a new state of type {stateType.FullName} by reflection (maybe you don't any parameterless constructor ?)");
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("MongoDbEventStore.GetRehydratedAggregateAsync() : Cannot find property/field that manage state for aggregate" +
                    $" type {aggregateType.FullName}. State should be a property or a field of the aggregate");
            }
            List<IDomainEvent> events = new List<IDomainEvent>();
#if NETSTANDARD2_0
            events = await GetAllEventsByAggregateId(aggregateType, aggregateUniqueId).ToList().ConfigureAwait(false);
#elif NETSTANDARD2_1
            await foreach(var @event in GetAllEventsByAggregateId(aggregateType, aggregateUniqueId))
            {
                events.Add(@event);
            }
#endif
            state.ApplyRange(events);
            return state;
        }

        private async Task PrepareForEventInsertionAsync(IDomainEvent @event)
        {
            var evtType = @event.GetType();
            ISnapshotBehavior? behavior = _snapshotBehaviorProvider?.GetBehaviorForEventType(evtType);
            CheckIdAndSetNewIfNeeded(@event);
            ulong? sequence = null;
            if (@event.AggregateId != null)
            {
                await SetSequenceAsync(@event, sequence).ConfigureAwait(false);
                if (_archiveBehavior != SnapshotEventsArchiveBehavior.Disabled
                    && behavior?.IsSnapshotNeeded(@event) == true)
                {
                    var aggregateState = await GetRehydratedStateAsync(@event.AggregateId, @event.AggregateType).ConfigureAwait(false);
                    var result = behavior.GenerateSnapshot(aggregateState);
                    if (result?.Any() == true)
                    {
                        if (aggregateState != null)
                        {
                            await InsertSnapshotAsync(
                                new Snapshot(@event.AggregateId, @event.AggregateType, aggregateState, behavior.GetType(), DateTime.Now)
                                ).ConfigureAwait(false);
                        }
                        await StoreArchiveEventsAsync(result).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task StoreArchiveEventsAsync(IEnumerable<IDomainEvent> archiveEvents)
        {
            var filterBuilder = Builders<IDomainEvent>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(nameof(IDomainEvent.AggregateId), archiveEvents.First().AggregateId),
                filterBuilder.Eq(nameof(IDomainEvent.AggregateType), archiveEvents.First().AggregateType.AssemblyQualifiedName));

            switch (_archiveBehavior)
            {
                case SnapshotEventsArchiveBehavior.StoreToNewDatabase:
                    var otherDbArchiveCollection = await GetArchiveDatabaseEventCollectionAsync<IDomainEvent>().ConfigureAwait(false);
                    await otherDbArchiveCollection.InsertManyAsync(archiveEvents).ConfigureAwait(false);
                    break;
                case SnapshotEventsArchiveBehavior.StoreToNewTable:
                    var archiveCollection = await GetArchiveEventCollectionAsync<IDomainEvent>().ConfigureAwait(false);
                    await archiveCollection.InsertManyAsync(archiveEvents).ConfigureAwait(false);
                    break;
            }

            var deleteFilterBuilder = new FilterDefinitionBuilder<IDomainEvent>();
            var deleteFilter = deleteFilterBuilder.And(filter, deleteFilterBuilder.Lte(nameof(IDomainEvent.Sequence), archiveEvents.Max(e => e.Sequence)));
            await (await GetEventCollectionAsync<IDomainEvent>().ConfigureAwait(false)).DeleteManyAsync(deleteFilter).ConfigureAwait(false);
        }

        private async Task InsertSnapshotAsync(Snapshot snapshot)
        {
            var snapshotCollection = await GetSnapshotCollectionAsync().ConfigureAwait(false);

            var filterBuilder = Builders<Snapshot>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(nameof(Snapshot.AggregateId), snapshot.AggregateId),
                filterBuilder.Eq(nameof(Snapshot.AggregateType), snapshot.AggregateType));

            await snapshotCollection.FindOneAndDeleteAsync(filter).ConfigureAwait(false);

            await snapshotCollection.InsertOneAsync(snapshot).ConfigureAwait(false);
        }

        private async Task<Snapshot> GetSnapshotFromAggregateId<TId>(TId aggregateId, Type aggregateType)
            where TId : notnull
        {
            var filterBuilder = Builders<Snapshot>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(nameof(Snapshot.AggregateType), aggregateType.AssemblyQualifiedName),
                filterBuilder.Eq(nameof(Snapshot.AggregateId), (object)aggregateId));

            var snapshotCollection = await GetSnapshotCollectionAsync().ConfigureAwait(false);
            return await snapshotCollection.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        private async Task<IMongoCollection<T>> GetEventCollectionAsync<T>()
            where T : IDomainEvent
        {
            var collection = EventStoreManager.Client
                      .GetDatabase(Consts.CONST_DB_NAME)
                      .GetCollection<T>(Consts.CONST_EVENTS_COLLECTION_NAME);

            await CreateEventCollectionDefaultIndexes(collection).ConfigureAwait(false);

            return collection;
        }

        private async Task<IMongoCollection<T>> GetArchiveDatabaseEventCollectionAsync<T>()
            where T : IDomainEvent
        {
            var collection = EventStoreManager.Client
                .GetDatabase(Consts.CONST_ARCHIVE_DB_NAME)
                .GetCollection<T>(Consts.CONST_ARCHIVE_EVENTS_COLLECTION_NAME);

            await CreateEventCollectionDefaultIndexes(collection).ConfigureAwait(false);

            return collection;
        }

        private async Task<IMongoCollection<T>> GetArchiveEventCollectionAsync<T>()
            where T : IDomainEvent
        {
            var collection = EventStoreManager.Client
                      .GetDatabase(Consts.CONST_DB_NAME)
                      .GetCollection<T>(Consts.CONST_ARCHIVE_EVENTS_COLLECTION_NAME);

            await CreateEventCollectionDefaultIndexes(collection).ConfigureAwait(false);

            return collection;
        }

        private async Task CreateEventCollectionDefaultIndexes<T>(IMongoCollection<T> collection)
            where T : IDomainEvent
        {
            await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<T>(Builders<T>.IndexKeys
                    .Ascending(nameof(IDomainEvent.AggregateId))
                    .Ascending(nameof(IDomainEvent.AggregateType))
                    .Ascending(nameof(IDomainEvent.Sequence)))
                ).ConfigureAwait(false);
        }

        private async Task<IMongoCollection<Snapshot>> GetSnapshotCollectionAsync()
        {
            var collection = EventStoreManager.Client
                .GetDatabase(Consts.CONST_DB_NAME)
                .GetCollection<Snapshot>(Consts.CONST_SNAPSHOT_COLLECTION_NAME);

            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<Snapshot>(Builders<Snapshot>.IndexKeys
                                                                    .Ascending(nameof(Snapshot.AggregateId))
                                                                    .Ascending(nameof(Snapshot.AggregateType))
                                                                    .Ascending(nameof(Snapshot.SnapshotTime)))
                ).ConfigureAwait(false);

            return collection;
        }

        private async Task SetSequenceAsync(IDomainEvent @event, ulong? sequence = null)
        {
            if (@event.Sequence == 0 && @event.AggregateId != null)
            {
                ulong currentSeq = 0;
                if (sequence.HasValue)
                {
                    currentSeq = sequence.Value;
                }
                else
                {
                    async Task<long> RetrieveCurrentSequenceFromDbAsync()
                    {
                        var filter = Builders<IDomainEvent>.Filter.Eq(nameof(IDomainEvent.AggregateId), @event.AggregateId);
                        var collection = await GetEventCollectionAsync<IDomainEvent>().ConfigureAwait(false);
                        var filteredCollection = await collection.FindAsync(filter).ConfigureAwait(false);
                        var maxSequence = filteredCollection.ToList().Max(e => (ulong?)e.Sequence) ?? 0;
                        return Convert.ToInt64(maxSequence);
                    }
                    currentSeq = (ulong)(await RetrieveCurrentSequenceFromDbAsync().ConfigureAwait(false));
                    currentSeq++;
                }
                if (@event is BaseDomainEvent bde)
                {
                    bde.Sequence = currentSeq;
                }
                else
                {
                    var sequenceProp = @event.GetType().GetAllProperties().FirstOrDefault(m => m.Name == nameof(IDomainEvent.Sequence));
                    if (sequenceProp?.SetMethod != null)
                    {
                        sequenceProp.SetValue(@event, Convert.ToUInt64(currentSeq));
                    }
                    //TODO we preferably must log warning here
                }
            }
        }

        private void CheckIdAndSetNewIfNeeded(IDomainEvent @event)
        {
            if (@event.Id == Guid.Empty)
            {
                var idProp = @event.GetType().GetAllProperties().FirstOrDefault(p => p.Name == nameof(IDomainEvent.Id));
                if (idProp?.SetMethod != null)
                {
                    idProp.SetValue(@event, Guid.NewGuid());
                }
            }
        }

        #endregion

    }
}
