using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.Dispatcher;
using CQELight.EventStore.EFCore.Common;
using CQELight.IoC;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.EventStore.EFCore
{
    internal static class EventStoreManager
    {
        #region Internal static properties

        internal static EFEventStoreOptions s_Options;

        private static readonly ILogger _logger;
        private static readonly ILoggerFactory _loggerFactory;

        #endregion

        #region Static accessor

        static EventStoreManager()
        {
            if (DIManager.IsInit)
            {
                _loggerFactory = DIManager.BeginScope().Resolve<ILoggerFactory>();
                _logger = _loggerFactory?.CreateLogger("EventStore");
            }
            else
            {
                _loggerFactory = new LoggerFactory();
                _loggerFactory.AddProvider(new DebugLoggerProvider());
                _logger = _loggerFactory.CreateLogger(nameof(EventStoreManager));
            }
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
                await new EFEventStore(s_Options, _loggerFactory).StoreDomainEventAsync(@event).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                _logger?.LogError($"EventHandler.OnEventDispatchedMethod() : Exception {exc}");
            }
        }
        
        internal static async Task OnEventsDispatchedMethod(IEnumerable<IDomainEvent> events)
        {
            try
            {
                await new EFEventStore(s_Options, _loggerFactory).StoreDomainEventRangeAsync(events).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                _logger?.LogError($"EventHandler.OnEventsDispatchedMethod() : Exception {exc}");
            }
        }

        #endregion

    }
}
