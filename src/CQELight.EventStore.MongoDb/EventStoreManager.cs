using CQELight.Abstractions.Events.Interfaces;
using CQELight.Dispatcher;
using CQELight.IoC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CQELight.EventStore.MongoDb
{
    internal static class EventStoreManager
    {
        #region Static members

        private static MongoClient? s_client;
        private static bool s_Active;

        #endregion

        #region Internal static properties

        internal static MongoEventStoreOptions Options = default!;

        internal static MongoClient Client
        {
            get
            {
                if (!s_Active)
                {
                    throw new InvalidOperationException("MongoDbEventStore has been deactivated. Therefore, access to its Client is impossible.");
                }
                return s_client ??= new MongoClient(ExtractUrlFromOptions());
            }
            set
            {
                s_client = value;
            }
        }

        private static readonly ILogger? _logger =
            DIManager.IsInit
            ? DIManager.BeginScope().Resolve<ILoggerFactory>()?.CreateLogger(nameof(EventStoreManager))
            : new LoggerFactory(new[] { new DebugLoggerProvider() }).CreateLogger(nameof(EventStoreManager));

        #endregion

        #region Private static methods

        private static MongoUrl ExtractUrlFromOptions()
        {
            var urlBuilder = new MongoUrlBuilder
            {
                Servers = Options.ServerUrls.Select(u => new MongoServerAddress(u)),
                Username = Options.Username,
                Password = Options.Password
            };
            return urlBuilder.ToMongoUrl();
        }

        #endregion

        #region Internal static methods

        internal static void Activate()
        {
            CoreDispatcher.OnEventDispatched += OnEventDispatchedMethod;
            Client = new MongoClient(ExtractUrlFromOptions());
            s_Active = true;
        }

        internal static void Deactivate()
        {
            CoreDispatcher.OnEventDispatched -= OnEventDispatchedMethod;
            s_Active = false;
        }

        internal static async Task OnEventDispatchedMethod(IDomainEvent @event)
        {
            if (Client != null)
            {
                try
                {
                    await new MongoDbEventStore(Options.SnapshotBehaviorProvider).StoreDomainEventAsync(@event).ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    _logger?.LogError($"EventHandler.OnEventDispatchedMethod() : Exception {exc}");
                }
            }
        }

        #endregion

    }
}
