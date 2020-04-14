using CQELight.Abstractions.Events.Interfaces;
using CQELight.Dispatcher;
using CQELight.EventStore.EFCore.Common;
using CQELight.IoC;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CQELight.EventStore.EFCore
{
    internal static class EventStoreManager
    {
        #region Internal static properties

        internal static EFEventStoreOptions s_Options = default!;

        private static readonly ILogger s_Logger;
        private static readonly ILoggerFactory s_LoggerFactory = default!;

        #endregion

        #region Static accessor

        static EventStoreManager()
        {
            if (DIManager.IsInit)
            {
                s_LoggerFactory = DIManager.BeginScope().Resolve<ILoggerFactory>();
            }
            if (s_LoggerFactory == null)
            {
                s_LoggerFactory = new LoggerFactory(new[] { new DebugLoggerProvider() });
            }
            s_Logger = s_LoggerFactory.CreateLogger(nameof(EventStoreManager));
        }

        #endregion

        #region Public static methods

        internal static void Activate()
        {
            CoreDispatcher.OnEventDispatched += OnEventDispatchedMethod;
            CoreDispatcher.OnEventsDispatched += OnEventsDispatchedMethod;

            using (var ctx = new EventStoreDbContext(s_Options.DbContextOptions, s_Options.ArchiveBehavior))
            {
                ctx.Database.Migrate();
            }
            if (s_Options.ArchiveBehavior == SnapshotEventsArchiveBehavior.StoreToNewDatabase
                && s_Options.ArchiveDbContextOptions != null)
            {
                using (var ctx = new ArchiveEventStoreDbContext(s_Options.ArchiveDbContextOptions))
                {
                    ctx.Database.Migrate();
                }
            }
        }

        internal static void Deactivate()
        {
            CoreDispatcher.OnEventDispatched -= OnEventDispatchedMethod;
            CoreDispatcher.OnEventsDispatched -= OnEventsDispatchedMethod;
        }

        internal static async Task OnEventDispatchedMethod(IDomainEvent @event)
        {
            try
            {
                await new EFEventStore(s_Options, s_LoggerFactory).StoreDomainEventAsync(@event).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                s_Logger?.LogError($"EventHandler.OnEventDispatchedMethod() : Exception {exc}");
            }
        }

        internal static async Task OnEventsDispatchedMethod(IEnumerable<IDomainEvent> events)
        {
            try
            {
                await new EFEventStore(s_Options, s_LoggerFactory).StoreDomainEventRangeAsync(events).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                s_Logger?.LogError($"EventHandler.OnEventsDispatchedMethod() : Exception {exc}");
            }
        }

        #endregion

    }
}
