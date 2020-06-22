using CQELight.Abstractions.DDD;
using System;

namespace CQELight.EventStore.MongoDb.Models
{
    internal class Snapshot
    {
        #region Properties

#pragma warning disable RCS1170 // Use read-only auto-implemented property.
        public Guid Id { get; private set; }
        public AggregateState AggregateState { get; private set; } = default!;
        public string SnapshotBehaviorType { get; private set; } = default!;
        public DateTime SnapshotTime { get; private set; }
        public object AggregateId { get; private set; } = default!;
        public string AggregateType { get; private set; } = default!;
#pragma warning restore RCS1170 // Use read-only auto-implemented property.

        #endregion

        #region Ctor

        [Obsolete("Used for deserialization only")]
        public Snapshot() { }

        public Snapshot(object aggregateId, Type aggregateType, AggregateState aggregateState, Type snapshotBehaviorType, DateTime snapshotTime)
            : this(Guid.NewGuid(), aggregateId, aggregateType, aggregateState, snapshotBehaviorType, snapshotTime)
        {
        }

        public Snapshot(Guid id, object aggregateId, Type aggregateType, AggregateState aggregateState, Type snapshotBehaviorType, DateTime snapshotTime)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(aggregateType));
            AggregateState = aggregateState ?? throw new ArgumentNullException(nameof(aggregateState));

            SnapshotBehaviorType = snapshotBehaviorType.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(snapshotBehaviorType));
            SnapshotTime = snapshotTime;

            Id = id;
        }

        #endregion

    }
}
