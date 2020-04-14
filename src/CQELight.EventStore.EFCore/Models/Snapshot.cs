using System;

namespace CQELight.EventStore.EFCore.Models
{
    internal class Snapshot
    {
        #region Properties

        public virtual Guid Id { get; set; }
        public virtual int HashedAggregateId { get; set; }
        public virtual string AggregateType { get; set; } = "";
        public virtual string SnapshotData { get; set; } = "";
        public virtual DateTime SnapshotTime { get; set; }
        public virtual string SnapshotBehaviorType { get; set; } = "";

        #endregion

    }
}
