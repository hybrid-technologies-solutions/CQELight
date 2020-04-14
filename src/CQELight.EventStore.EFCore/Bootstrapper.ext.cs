using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore.EFCore;
using CQELight.EventStore.EFCore.Common;
using CQELight.IoC;
using System;

namespace CQELight
{
    public static class BootstrapperExt
    {
        #region Extension methods

        /// <summary>
        /// Use EF Core as EventStore for system, with the provided connection string.
        /// This is a usable case for common cases, in system that not have a huge amount of events, or for test/debug purpose.
        /// This is not recommanded for real-world big applications case.
        /// </summary>
        /// <param name="bootstrapper">Bootstrapper instance.</param>
        /// <param name="options">Options to use to configure event store.</param>
        /// <returns>Bootstrapper instance</returns>
        public static Bootstrapper UseEFCoreAsEventStore(this Bootstrapper bootstrapper, EFEventStoreOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var service = new EFEventStoreBootstrappService
            (ctx =>
                {
                    if (ctx.IsServiceRegistered(BootstrapperServiceType.IoC))
                    {
                        bootstrapper.AddIoCRegistration(new FactoryRegistration(() =>
                            new EventStoreDbContext(options.DbContextOptions, options.ArchiveBehavior), typeof(EventStoreDbContext)));
                        bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(EFEventStore), true));
                        bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(options, typeof(EFEventStoreOptions)));
                        if (options.SnapshotBehaviorProvider != null)
                        {
                            bootstrapper.AddIoCRegistration(
                                new InstanceTypeRegistration(options.SnapshotBehaviorProvider, typeof(ISnapshotBehaviorProvider)));
                        }
                        if (options.ArchiveBehavior == EventStore.SnapshotEventsArchiveBehavior.StoreToNewDatabase
                        && options.ArchiveDbContextOptions != null)
                        {
                            bootstrapper.AddIoCRegistration(new FactoryRegistration(() =>
                                new ArchiveEventStoreDbContext(options.ArchiveDbContextOptions), typeof(ArchiveEventStoreDbContext)));
                        }
                    }
                    EventStoreManager.s_Options = options;
                    EventStoreManager.Activate();
                }
            );
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        #endregion
    }
}
