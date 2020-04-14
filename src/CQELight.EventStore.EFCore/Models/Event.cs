using System;

namespace CQELight.EventStore.EFCore.Models
{
    internal class Event
    {
        #region Properties

        public virtual Guid Id { get; set; }
        public virtual string? SerializedAggregateId { get; set; }
        public virtual int? HashedAggregateId { get; set; }
        public virtual string? AggregateIdType { get; set; }
        public virtual string? AggregateType { get; set; }
        public virtual string EventData { get; set; } = "";
        public virtual string EventType { get; set; } = "";
        public virtual DateTime EventTime { get; set; }
        public virtual long Sequence { get; set; }

        #endregion

    }
}
