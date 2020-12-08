using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using System;

namespace CQELight.EventStore.EFCore
{
    /// <summary>
    /// Options for EF Core as Event Store.
    /// </summary>
    public class EFEventStoreOptions
    {
        #region Properties

        /// <summary>
        /// Instance of snapshot behavior provider.
        /// </summary>
        public ISnapshotBehaviorProvider? SnapshotBehaviorProvider { get; }
        /// <summary>
        /// Options for DbContext configuration.
        /// </summary>
        public DbContextOptions<EventStoreDbContext> DbContextOptions { get; }
        /// <summary>
        /// Informations about using buffer or not.
        /// </summary>
        public BufferInfo BufferInfo { get; }
        /// <summary>
        /// Event archive behavior to apply when generating
        /// snapshot.
        /// </summary>
        public SnapshotEventsArchiveBehavior ArchiveBehavior { get; }
        /// <summary>
        /// DbContext options for archive behavior.
        /// </summary>
        public DbContextOptions<ArchiveEventStoreDbContext>? ArchiveDbContextOptions { get; }
        /// <summary>
        /// Flag that defines if non aggregate events should be persisted.
        /// </summary>
        public bool ShouldPersistNonAggregateEvent { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of the options class.
        /// </summary>
        /// <param name="mainDbContextOptionsBuilderCfg">Options for DbContext configuration</param>
        /// <param name="snapshotBehaviorProvider">Provider of snapshot behaviors</param>
        /// <param name="bufferInfo">Buffer info to use. Disabled by default.</param>
        /// <param name="archiveBehavior">Behavior to adopt when creating a snapshot</param>
        /// <param name="archiveDbContextOptionsBuilderCfg">Configuration to apply to access archive events</param>
        /// <param name="shouldPersistNonAggregateEvent">A flag that indicates if non aggregate event (events that doesn't have AggregateType nor AggregateId defined) should be saved</param>
        /// A value is needed if <paramref name="snapshotBehaviorProvider"/> is provided and
        /// <paramref name="archiveBehavior"/> is set to StoreToNewDatabase.
        /// </param>
        public EFEventStoreOptions(
            Action<DbContextOptionsBuilder<EventStoreDbContext>> mainDbContextOptionsBuilderCfg,
            ISnapshotBehaviorProvider? snapshotBehaviorProvider = null,
            BufferInfo? bufferInfo = null,
            SnapshotEventsArchiveBehavior? archiveBehavior = null,
            Action<DbContextOptionsBuilder<ArchiveEventStoreDbContext>>? archiveDbContextOptionsBuilderCfg = null,
            bool shouldPersistNonAggregateEvent = true)
        {
            if (mainDbContextOptionsBuilderCfg == null)
            {
                throw new ArgumentNullException(nameof(mainDbContextOptionsBuilderCfg));
            }
            if (archiveBehavior == SnapshotEventsArchiveBehavior.StoreToNewDatabase && archiveDbContextOptionsBuilderCfg == null)
            {
                throw new ArgumentException("A DbContextOptions should be provided to access archive database cause " +
                    "SnapshotEventsArchiveBehavior is set to StoreToNewDatabase.");
            }
            var mainDbContextOptionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
            mainDbContextOptionsBuilderCfg(mainDbContextOptionsBuilder);
            DbContextOptions = mainDbContextOptionsBuilder.Options;

            SnapshotBehaviorProvider = snapshotBehaviorProvider;
            BufferInfo = bufferInfo ?? BufferInfo.Disabled;
            if (archiveBehavior.HasValue && snapshotBehaviorProvider != null && archiveDbContextOptionsBuilderCfg != null)
            {
                var archiveDbContextOptionsBuilder = new DbContextOptionsBuilder<ArchiveEventStoreDbContext>();
                archiveDbContextOptionsBuilderCfg(archiveDbContextOptionsBuilder);
                ArchiveBehavior = archiveBehavior.Value;
                ArchiveDbContextOptions = archiveDbContextOptionsBuilder.Options;
            }
            ShouldPersistNonAggregateEvent = shouldPersistNonAggregateEvent;
        }

        #endregion

    }
}
