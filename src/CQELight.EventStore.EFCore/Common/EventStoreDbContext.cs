using CQELight.EventStore.EFCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CQELight.EventStore.EFCore.Common
{
    public class EventStoreDbContext : DbContext
    {
        #region Members

        private readonly SnapshotEventsArchiveBehavior _behavior;

        #endregion

        #region Ctor

        public EventStoreDbContext(DbContextOptions<EventStoreDbContext> contextOptions)
            : base(contextOptions)
        {
            _behavior = SnapshotEventsArchiveBehavior.StoreToNewTable;
        }

        public EventStoreDbContext(DbContextOptions<EventStoreDbContext> contextOptions, SnapshotEventsArchiveBehavior behavior)
            : base(contextOptions)
        {
            _behavior = behavior;
        }

        #endregion

        #region Overriden methods

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var evtModel = modelBuilder.Entity<Event>();

            evtModel.HasIndex(e => new { e.HashedAggregateId, e.AggregateType });
            evtModel.HasIndex(e => new { e.EventType });

            evtModel.HasKey(e => e.Id);
            evtModel.Property(e => e.AggregateType).HasMaxLength(1024);
            evtModel.Property(e => e.EventData).IsRequired();
            evtModel.Property(e => e.EventType).IsRequired().HasMaxLength(1024);
            evtModel.Property(e => e.EventTime).IsRequired();
            evtModel.Property(e => e.Sequence).IsRequired();

            var snapModel = modelBuilder.Entity<Snapshot>();

            snapModel.HasIndex(e => new { e.HashedAggregateId, e.AggregateType });

            snapModel.HasKey(e => e.Id);
            snapModel.Property(e => e.AggregateType).HasMaxLength(1024);
            snapModel.Property(e => e.SnapshotData).IsRequired();
            snapModel.Property(e => e.SnapshotTime).IsRequired();

            if (_behavior == SnapshotEventsArchiveBehavior.StoreToNewTable)
            {
                modelBuilder.ApplyConfiguration(new EventArchiveEntityTypeConfiguration());
            }
        }

        #endregion

    }
}
