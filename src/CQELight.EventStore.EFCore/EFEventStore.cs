using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore.Attributes;
using CQELight.EventStore.EFCore.Common;
using CQELight.EventStore.EFCore.Models;
using CQELight.EventStore.EFCore.Serialisation;
using CQELight.Tools.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.EventStore.EFCore
{
    internal class EFEventStore : IEventStore, IAggregateEventStore
    {
        #region Static members

        private static DateTime? s_BufferEnteredTimeAbsolute;
        private static DateTime? s_BufferEnteredTimeSliding;
        private static Timer? s_AbsoluteTimer;
        private static Timer? s_SlidingTimer;

        private static readonly SemaphoreSlim s_Lock = new SemaphoreSlim(2);
        private static ConcurrentBag<Event> s_Events = new ConcurrentBag<Event>();

        #endregion

        #region Members

        private readonly ILogger _logger;
        private readonly ISnapshotBehaviorProvider? _snapshotBehaviorProvider;
        private readonly BufferInfo _bufferInfo;
        private readonly SnapshotEventsArchiveBehavior _archiveBehavior;
        private readonly DbContextOptions<ArchiveEventStoreDbContext>? _archiveBehaviorDbContextOptions;
        private readonly DbContextOptions<EventStoreDbContext> _dbContextOptions;
        private readonly bool shouldPersistNonAggregateEvents;

        #endregion

        #region Ctor

        public EFEventStore(EFEventStoreOptions options, ILoggerFactory? loggerFactory = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _dbContextOptions = options.DbContextOptions;
            _logger = (loggerFactory ?? new LoggerFactory(new[] { new DebugLoggerProvider() })).CreateLogger<EFEventStore>();
            _snapshotBehaviorProvider = options.SnapshotBehaviorProvider;

            _bufferInfo = options.BufferInfo;
            _archiveBehavior = options.ArchiveBehavior;
            _archiveBehaviorDbContextOptions = options.ArchiveDbContextOptions;
            shouldPersistNonAggregateEvents = options.ShouldPersistNonAggregateEvent;

            if (_bufferInfo != null)
            {
                s_SlidingTimer = new Timer(TreatBufferEvents, null, Timeout.Infinite, Timeout.Infinite);
                s_AbsoluteTimer = new Timer(TreatBufferEvents, null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        #endregion

        #region IEventStore

#if NETSTANDARD2_1 || NET5_0
        public async IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateType(Type aggregateType)
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.AggregateType == aggregateType.AssemblyQualifiedName)
                    .AsAsyncEnumerable();
                await foreach (var @event in dbEvents)
                {
                    yield return GetRehydratedEventFromDbEvent(@event);
                }
            }
        }

        public async IAsyncEnumerable<T> GetAllEventsByEventType<T>()
            where T : class, IDomainEvent
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.EventType == typeof(T).AssemblyQualifiedName)
                    .AsAsyncEnumerable();
                await foreach (var @event in dbEvents)
                {
                    if (GetRehydratedEventFromDbEvent(@event) is T rehydratedEvent)
                    {
                        yield return rehydratedEvent;
                    }
                }
            }
        }

        public async IAsyncEnumerable<IDomainEvent> GetAllEventsByEventType(Type eventType)
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.EventType == eventType.AssemblyQualifiedName)
                    .AsAsyncEnumerable();
                await foreach (var @event in dbEvents)
                {
                    yield return GetRehydratedEventFromDbEvent(@event);
                }
            }
        }
        public async IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId(Type aggregateType, object aggregateId)
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.AggregateType == aggregateType.AssemblyQualifiedName
                    && c.AggregateIdType == aggregateId.GetType().AssemblyQualifiedName
                    && c.HashedAggregateId == aggregateId.ToJson(true).GetHashCode())
                    .AsAsyncEnumerable();
                await foreach (var @event in dbEvents)
                {
                    yield return GetRehydratedEventFromDbEvent(@event);
                }
            }
        }
#elif NETSTANDARD2_0
        public IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateType(Type aggregateType)
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.AggregateType == aggregateType.AssemblyQualifiedName)
                    .ToList();
                return dbEvents.Select(GetRehydratedEventFromDbEvent).ToAsyncEnumerable();
            }
        }

        public IAsyncEnumerable<T> GetAllEventsByEventType<T>()
            where T : class, IDomainEvent
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.EventType == typeof(T).AssemblyQualifiedName)
                    .ToList();
                return dbEvents.Select(e => (T)GetRehydratedEventFromDbEvent(e)).WhereNotNull().ToAsyncEnumerable();
            }
        }

        public IAsyncEnumerable<IDomainEvent> GetAllEventsByEventType(Type eventType)
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.EventType == eventType.AssemblyQualifiedName)
                    .ToList();
                return dbEvents.Select(GetRehydratedEventFromDbEvent).ToAsyncEnumerable();
            }
        }

        public IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId(Type aggregateType, object aggregateId)
        {
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var dbEvents = ctx
                    .Set<Event>()
                    .AsNoTracking()
                    .Where(c => c.AggregateType == aggregateType.AssemblyQualifiedName
                    && c.AggregateIdType == aggregateId.GetType().AssemblyQualifiedName
                    && c.HashedAggregateId == aggregateId.ToJson(true).GetHashCode())
                    .ToList();
                return dbEvents.Select(GetRehydratedEventFromDbEvent).ToAsyncEnumerable();
            }
        }
#endif

        public IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId<TAggregateType, TAggregateId>(TAggregateId id)
            where TAggregateType : AggregateRoot<TAggregateId>
            where TAggregateId : notnull
        {
            return GetAllEventsByAggregateId(typeof(TAggregateType), id);
        }

        public async Task<Result> StoreDomainEventAsync(IDomainEvent @event)
        {
            var evtType = @event.GetType();
            if (evtType.IsDefined(typeof(EventNotPersistedAttribute)))
            {
                return Result.Ok();
            }
            if (!shouldPersistNonAggregateEvents && (@event.AggregateId == null || @event.AggregateType == null))
            {
                return Result.Ok();
            }
            try
            {
                if (_bufferInfo?.UseBuffer == true)
                {
                    await s_Lock.WaitAsync().ConfigureAwait(false);
                }
                using (var ctx = new EventStoreDbContext(_dbContextOptions, _archiveBehavior))
                {
                    await StoreEvent(@event, ctx).ConfigureAwait(false);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogErrorMultilines("EFEventStore : Unable to persist an event.", e.ToString());
                return Result.Fail();
            }
            finally
            {
                s_Lock.Release();
            }
            return Result.Ok();
        }

        public async Task<Result> StoreDomainEventRangeAsync(IEnumerable<IDomainEvent> events)
        {
            if (events?.Any() == false)
            {
                return Result.Fail();
            }

            try
            {
                var eventsByAggregateGroup
                    = events
                        .GroupBy(e => e.AggregateId ?? 0)
                        .Select(g => new { AggregateId = g.Key, Events = g.ToList() })
                        .ToList();
                var tasks = new List<Task>();
                foreach (var evtGroups in eventsByAggregateGroup)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        using (var ctx = new EventStoreDbContext(_dbContextOptions, _archiveBehavior))
                        {
                            foreach (var evt in evtGroups.Events)
                            {
                                await StoreEvent(evt, ctx, false).ConfigureAwait(false);
                            }
                            await ctx.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogErrorMultilines("EFEventStore : Unable to persist a collection of events.", e.ToString());
                return Result.Fail();
            }
            return Result.Ok();
        }

        #endregion

        #region IAggregateEventStore

        public async Task<IEventSourcedAggregate> GetRehydratedAggregateAsync(
            object aggregateUniqueId, Type aggregateType)
        {
            if (aggregateUniqueId == null)
            {
                throw new ArgumentNullException(nameof(aggregateUniqueId));
            }
            if (!(aggregateType.CreateInstance() is IEventSourcedAggregate aggInstance))
            {
                throw new InvalidOperationException("EFEventStore.GetRehydratedAggregateAsync() : Cannot create a new instance of" +
                    $" {aggregateType.FullName} aggregate. It should have one parameterless constructor (can be private).");
            }
            Snapshot? snapshot = null;
            using (var ctx = new EventStoreDbContext(_dbContextOptions, _archiveBehavior))
            {
                var hashedAggregateId = aggregateUniqueId.ToJson(true).GetHashCode();
                snapshot = await ctx.Set<Snapshot>()
                    .Where(t => t.AggregateType == aggregateType.AssemblyQualifiedName && t.HashedAggregateId == hashedAggregateId)
                    .FirstOrDefaultAsync().ConfigureAwait(false);
            }
            PropertyInfo? stateProp = aggregateType.GetAllProperties().FirstOrDefault(p => p.PropertyType.IsSubclassOf(typeof(AggregateState)));
            FieldInfo? stateField = aggregateType.GetAllFields().FirstOrDefault(f => f.FieldType.IsSubclassOf(typeof(AggregateState)));
            Type? stateType = stateProp?.PropertyType ?? stateField?.FieldType;
            if (stateType != null)
            {
                AggregateState? state = stateType.CreateInstance() as AggregateState;
                if (snapshot != null)
                {
                    state = snapshot.SnapshotData.FromJson(stateType) as AggregateState;
                    if (state == null)
                    {
                        throw new InvalidOperationException("EFEventStore.GetRehydratedAggregateAsync() : Cannot retrieve a valid state from snapshot in database.");
                    }
                }
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
                throw new InvalidOperationException("EFEventStore.GetRehydratedAggregateAsync() : Cannot find property/field that manage state for aggregate" +
                    $" type {aggregateType.FullName}. State should be a property or a field of the aggregate");
            }
            List<IDomainEvent> events = new List<IDomainEvent>();
#if NETSTANDARD2_0
            events = await
                 GetAllEventsByAggregateId(aggregateType, aggregateUniqueId)
                .ToList().ConfigureAwait(false);
#elif NETSTANDARD2_1 || NET5_0
            await foreach(var @event in GetAllEventsByAggregateId(aggregateType, aggregateUniqueId))
            {
                events.Add(@event);
            }
#endif
            aggInstance.RehydrateState(events);
            return aggInstance;
        }

        public async Task<TAggregate> GetRehydratedAggregateAsync<TAggregate>(object aggregateUniqueId)
            where TAggregate : class, IEventSourcedAggregate
        {
            return (TAggregate)(await GetRehydratedAggregateAsync(aggregateUniqueId, typeof(TAggregate)).ConfigureAwait(false));
        }

        #endregion

        #region Private methods

        private async Task StoreEvent(IDomainEvent @event, EventStoreDbContext ctx, bool useBuffer = true)
        {
            var sequence = Convert.ToInt64(@event.Sequence);
            if (@event.AggregateId != null && @event.AggregateType != null)
            {
                var hashedAggregateId = @event.AggregateId.ToJson(true).GetHashCode();
                if (sequence == 0)
                {
                    sequence = await ComputeEventSequence(ctx, useBuffer, hashedAggregateId, @event.AggregateType).ConfigureAwait(false);
                    if (@event is BaseDomainEvent baseDomainEvent)
                    {
                        baseDomainEvent.Sequence = Convert.ToUInt64(sequence);
                    }
                    else
                    {
                        @event.GetType().GetAllProperties()
                            .First(p => p.Name == nameof(IDomainEvent.Sequence)).SetMethod?.Invoke(@event, new object[] { Convert.ToUInt64(sequence) });
                    }
                }
                await ManageSnapshotBehavior(@event, ctx, hashedAggregateId).ConfigureAwait(false);
            }
            var persistableEvent = GetEventFromIDomainEvent(@event, sequence);
            if (_bufferInfo?.UseBuffer == true && useBuffer)
            {
                s_Events.Add(persistableEvent);

                if (!s_BufferEnteredTimeAbsolute.HasValue && !s_BufferEnteredTimeSliding.HasValue)
                {
                    s_BufferEnteredTimeAbsolute = DateTime.Now;
                    s_BufferEnteredTimeSliding = DateTime.Now;
                    s_AbsoluteTimer!.Change(Convert.ToInt32(_bufferInfo.AbsoluteTimeOut.TotalMilliseconds), Timeout.Infinite);
                    s_SlidingTimer!.Change(Convert.ToInt32(_bufferInfo.SlidingTimeOut.TotalMilliseconds), Timeout.Infinite);
                }
                else
                {
                    if (DateTime.Now.Subtract(s_BufferEnteredTimeAbsolute ?? DateTime.MaxValue).TotalMilliseconds >= _bufferInfo.AbsoluteTimeOut.TotalMilliseconds)
                    {
                        TreatBufferEvents();
                        s_BufferEnteredTimeAbsolute = null;
                        s_BufferEnteredTimeSliding = null;
                    }
                    else if (DateTime.Now.Subtract(s_BufferEnteredTimeAbsolute ?? DateTime.MaxValue).TotalMilliseconds >= _bufferInfo.SlidingTimeOut.TotalMilliseconds)
                    {
                        TreatBufferEvents();
                        s_BufferEnteredTimeAbsolute = null;
                        s_BufferEnteredTimeSliding = null;
                    }
                    else
                    {
                        s_BufferEnteredTimeSliding = DateTime.Now;
                        s_SlidingTimer!.Change(Convert.ToInt32(_bufferInfo.SlidingTimeOut.TotalMilliseconds), Timeout.Infinite);
                    }
                }
            }
            else
            {
                ctx.Add(persistableEvent);
            }
        }

        private async Task<long> ComputeEventSequence(EventStoreDbContext ctx, bool useBuffer, int hashedAggregateId, Type aggregateType)
        {
            long currentSequence = 0;
            if (_bufferInfo?.UseBuffer == true && useBuffer)
            {
                currentSequence = s_Events.Max(e => (long?)e.Sequence) ?? 0;
            }
            if (currentSequence == 0)
            {
                var currentInMemoryEvents = ExtractEventsFromChangeTracker(ctx);
                if (currentInMemoryEvents.Count > 0) // We are currently storing a range of events, also a context is dedicated to one aggId
                {
                    currentSequence = Convert.ToInt64(currentInMemoryEvents.Max(e => e.Sequence));
                }
                else
                {
                    currentSequence = await ctx
                        .Set<Event>()
                        .AsNoTracking()
                        .Where(t => t.HashedAggregateId == hashedAggregateId && t.AggregateType == aggregateType.AssemblyQualifiedName)
                        .MaxAsync(e => (long?)e.Sequence)
                        .ConfigureAwait(false) ?? 0;
                }
            }
            return ++currentSequence;
        }

        private async Task ManageSnapshotBehavior(
            IDomainEvent @event,
            EventStoreDbContext ctx,
            int hashedAggregateId)
        {
            bool IsSnaphostEnabled()
            {
                return _snapshotBehaviorProvider != null
                        && _archiveBehavior != SnapshotEventsArchiveBehavior.Disabled;
            }
            if (@event.AggregateId != null && @event.AggregateType != null)
            {
                var evtType = @event.GetType();
                if (IsSnaphostEnabled())
                {
                    var behavior = _snapshotBehaviorProvider!.GetBehaviorForEventType(evtType);
                    if (behavior?.IsSnapshotNeeded(@event) == true)
                    {
                        var aggState = await GetRehydratedAggregateStateAsync(@event.AggregateId, @event.AggregateType, ctx)
                            .ConfigureAwait(false);

                        var eventsToArchive = behavior.GenerateSnapshot(aggState);

                        if (eventsToArchive?.Any() == true)
                        {
                            if (aggState != null)
                            {
                                var previousSnapshot = await ctx
                                    .Set<Snapshot>()
                                    .FirstOrDefaultAsync(s =>
                                        s.HashedAggregateId == hashedAggregateId &&
                                        s.AggregateType == @event.AggregateType.AssemblyQualifiedName)
                                    .ConfigureAwait(false);
                                if (previousSnapshot != null)
                                {
                                    ctx.Remove(previousSnapshot);
                                }
                                ctx.Add(new Snapshot
                                {
                                    AggregateType = @event.AggregateType.AssemblyQualifiedName,
                                    HashedAggregateId = hashedAggregateId,
                                    SnapshotBehaviorType = behavior.GetType().AssemblyQualifiedName,
                                    SnapshotTime = DateTime.Now,
                                    SnapshotData = aggState.ToJson(new AggregateStateSerialisationContract())
                                });
                            }
                            await StoreArchiveEventsAsync(eventsToArchive, ctx).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task<AggregateState> GetRehydratedAggregateStateAsync(
            object aggregateId,
            Type aggregateType,
            EventStoreDbContext? externalCtx = null)
        {
            List<IDomainEvent> events = new List<IDomainEvent>();
#if NETSTANDARD2_0
            events = await
                 GetAllEventsByAggregateId(aggregateType, aggregateId)
                .ToList().ConfigureAwait(false);
#elif NETSTANDARD2_1 || NET5_0
            await foreach (var @event in GetAllEventsByAggregateId(aggregateType, aggregateId))
            {
                events.Add(@event);
            }
#endif
            if (externalCtx != null)
            {
                var eventsInChangeTracker = ExtractEventsFromChangeTracker(externalCtx).Select(GetRehydratedEventFromDbEvent);
                events = events.Concat(eventsInChangeTracker).OrderBy(s => s.Sequence).ToList();
            }
            Snapshot? snapshot = null;
            using (var ctx = new EventStoreDbContext(_dbContextOptions, _archiveBehavior))
            {
                var hashedAggregateId = aggregateId.ToJson(true).GetHashCode();
                snapshot = await ctx.Set<Snapshot>()
                    .Where(t => t.AggregateType == aggregateType.AssemblyQualifiedName && t.HashedAggregateId == hashedAggregateId)
                    .FirstOrDefaultAsync().ConfigureAwait(false);
            }

            PropertyInfo? stateProp = aggregateType.GetAllProperties().FirstOrDefault(p => p.PropertyType.IsSubclassOf(typeof(AggregateState)));
            FieldInfo? stateField = aggregateType.GetAllFields().FirstOrDefault(f => f.FieldType.IsSubclassOf(typeof(AggregateState)));
            Type? stateType = stateProp?.PropertyType ?? stateField?.FieldType;
            AggregateState? state = null;
            if (stateType != null)
            {
                if (snapshot != null)
                {
                    state = snapshot.SnapshotData.FromJson(stateType) as AggregateState;
                    if (state == null)
                    {
                        throw new InvalidOperationException($"EFEventStore.GetRehydratedAggregateAsync() : Unable to retrieve state of type {stateType.FullName} from snapshot of id {snapshot.Id}");
                    }
                }
                else
                {
                    state = stateType.CreateInstance() as AggregateState;
                    if (state == null)
                    {
                        throw new InvalidOperationException($"EFEventStore.GetRehydratedAggregateAsync() : Unable to create a new state of type {stateType.FullName} by reflection (maybe you don't any parameterless constructor ?)");
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("EFEventStore.GetRehydratedAggregateAsync() : Cannot find property/field that manage state for aggregate" +
                    $" type {aggregateType.FullName}. State should be a property or a field of the aggregate");
            }

            state.ApplyRange(events);
            return state;
        }

        private async Task StoreArchiveEventsAsync(IEnumerable<IDomainEvent> archiveEvents, EventStoreDbContext externalCtx)
        {
            switch (_archiveBehavior)
            {
                case SnapshotEventsArchiveBehavior.StoreToNewTable:
                    using (var ctx = new EventStoreDbContext(_dbContextOptions))
                    {
                        ctx.AddRange(archiveEvents.Select(GetArchiveEventFromIDomainEvent).ToList());
                        await ctx.SaveChangesAsync().ConfigureAwait(false);
                    }
                    break;
                case SnapshotEventsArchiveBehavior.StoreToNewDatabase:
                    if (_archiveBehaviorDbContextOptions != null)
                    {
                        using (var ctx = new ArchiveEventStoreDbContext(_archiveBehaviorDbContextOptions))
                        {
                            ctx.AddRange(
                                archiveEvents.Select(GetArchiveEventFromIDomainEvent));
                            await ctx.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("EFEventStore.StoreDomainEventAsync() : Snapshot will be created and event to archive cannot be saved because DbContextOptions for archive database weren't provided during configuration on Bootstrapper. Either specify DbContextOptions for event's archive database or change archive behavior");
                    }
                    break;
            }
            var eventsInContext = externalCtx
                .ChangeTracker
                .Entries()
                .Where(e => e.State != EntityState.Detached && e.State != EntityState.Deleted)
                .Select(e => e.Entity as Event)
                .WhereNotNull()
                .Where(e => archiveEvents.Any(ev => ev.Id == e!.Id))
                .ToList();
            if (eventsInContext.Count > 0)
            {
                eventsInContext.DoForEach(e => externalCtx.Entry(e).State = EntityState.Detached);
            }
            using (var ctx = new EventStoreDbContext(_dbContextOptions))
            {
                var archiveEventsIds = archiveEvents.Select(e => e.Id).ToList();
                var events = await ctx.Set<Event>().Where(e => archiveEventsIds.Contains(e.Id)).ToListAsync().ConfigureAwait(false);
                if (events.Count > 0)
                {
                    ctx.RemoveRange(events);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        private void TreatBufferEvents(object? state = null)
        {
            s_Lock.Wait();
            {
                try
                {
                    if (s_Events.Count > 0)
                    {
                        using (var innerCtx = new EventStoreDbContext(_dbContextOptions, _archiveBehavior))
                        {
                            innerCtx.AddRange(s_Events);
                            s_Events = new ConcurrentBag<Event>();
                            innerCtx.SaveChanges();
                        }
                    }
                }
                finally
                {
                    s_BufferEnteredTimeAbsolute = null;
                    s_BufferEnteredTimeSliding = null;
                    s_AbsoluteTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    s_SlidingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    s_Lock.Release();
                }
            }
        }

        private IDomainEvent GetRehydratedEventFromDbEvent(Event evt)
        {
            var evtType = Type.GetType(evt.EventType);
            if (!(evt.EventData.FromJson(evtType) is IDomainEvent rehydratedEvt))
            {
                throw new InvalidOperationException($"Event {evt.Id} retrieved from store doesn't respects the IDomainEvent interface");
            }
            var properties = evtType.GetAllProperties();

            object? eventAggregateId = null;
            if (!string.IsNullOrWhiteSpace(evt.SerializedAggregateId) && !string.IsNullOrWhiteSpace(evt.AggregateIdType))
            {
                var aggregateIdType = Type.GetType(evt.AggregateIdType);
                if (aggregateIdType != null)
                {
                    eventAggregateId = Encoding.UTF8.GetString(
                            Convert.FromBase64String(evt.SerializedAggregateId)).FromJson(aggregateIdType);
                }
            }
            if (rehydratedEvt is BaseDomainEvent baseDomainEvent)
            {
                baseDomainEvent.Id = evt.Id;
                baseDomainEvent.EventTime = evt.EventTime;
                baseDomainEvent.Sequence = Convert.ToUInt64(evt.Sequence);
                baseDomainEvent.AggregateId = eventAggregateId;
            }
            else
            {
                properties.First(p => p.Name == nameof(IDomainEvent.Id)).SetMethod?.Invoke(rehydratedEvt, new object[] { evt.Id });
                properties.First(p => p.Name == nameof(IDomainEvent.EventTime)).SetMethod?.Invoke(rehydratedEvt, new object[] { evt.EventTime });
                properties.First(p => p.Name == nameof(IDomainEvent.Sequence)).SetMethod?.Invoke(rehydratedEvt, new object[] { Convert.ToUInt64(evt.Sequence) });
                if (eventAggregateId != null)
                {
                    properties.First(p => p.Name == nameof(IDomainEvent.AggregateId)).SetMethod?.Invoke(rehydratedEvt, new object[] { eventAggregateId });
                }
            }
            return rehydratedEvt;
        }

        private ArchiveEvent GetArchiveEventFromIDomainEvent(IDomainEvent @event)
        {
            var jsonId = @event.AggregateId != null ? @event.AggregateId.ToJson(true) : string.Empty;
            return new ArchiveEvent
            {
                Id = @event.Id != Guid.Empty ? @event.Id : Guid.NewGuid(),
                EventData = @event.ToJson(),
                HashedAggregateId = !string.IsNullOrWhiteSpace(jsonId) ? jsonId.GetHashCode() : new int?(),
                SerializedAggregateId = !string.IsNullOrWhiteSpace(jsonId) ? Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonId)) : string.Empty,
                AggregateIdType = @event.AggregateId?.GetType()?.AssemblyQualifiedName,
                AggregateType = @event.AggregateType?.AssemblyQualifiedName,
                EventType = @event.GetType().AssemblyQualifiedName,
                EventTime = @event.EventTime,
                Sequence = (long)@event.Sequence
            };
        }
        private Event GetEventFromIDomainEvent(IDomainEvent @event, long currentSeq)
        {
            var jsonId = @event.AggregateId != null ? @event.AggregateId.ToJson(true) : string.Empty;
            return new Event
            {
                Id = @event.Id != Guid.Empty ? @event.Id : Guid.NewGuid(),
                EventData = @event.ToJson(),
                HashedAggregateId = !string.IsNullOrWhiteSpace(jsonId) ? jsonId.GetHashCode() : new int?(),
                SerializedAggregateId = !string.IsNullOrWhiteSpace(jsonId) ? Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonId)) : string.Empty,
                AggregateIdType = @event.AggregateId?.GetType()?.AssemblyQualifiedName,
                AggregateType = @event.AggregateType?.AssemblyQualifiedName,
                EventType = @event.GetType().AssemblyQualifiedName,
                EventTime = @event.EventTime,
                Sequence = currentSeq
            };
        }

        private List<Event> ExtractEventsFromChangeTracker(EventStoreDbContext ctx)
        {
            return ctx.ChangeTracker
                               .Entries()
                               .Where(e => e.State != EntityState.Deleted && e.State != EntityState.Detached && e.Entity is Event)
                               .Select(e => (Event)e.Entity)
                               .WhereNotNull()
                               .ToList();
        }

        #endregion

    }
}
