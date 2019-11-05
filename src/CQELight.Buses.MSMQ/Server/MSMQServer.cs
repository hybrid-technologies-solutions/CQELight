using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.InMemory.Events;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.Buses.MSMQ.Client
{

    /// <summary>
    /// Server instance that pumps message from MSMQ and dispatch them within
    /// the system
    /// </summary>
    [Obsolete("MSMQ extension is no more supported and will be removed in V2")]
    public class MSMQServer : DisposableObject
    {

        #region Members

        private readonly ILogger _logger;
        private readonly string _emiter;
        private readonly InMemoryEventBus _inMemoryEventBus;
        private CancellationToken _token;
        private CancellationTokenSource _tokenSource;
        private readonly QueueConfiguration _configuration;
        private Task RunningTask;

        #endregion

        #region Ctor

        internal MSMQServer(string emiter, ILoggerFactory loggerFactory, InMemoryEventBus inMemoryEventBus = null,
            QueueConfiguration configuration = null)
        {
            if (string.IsNullOrWhiteSpace(emiter))
            {
                throw new ArgumentNullException(nameof(emiter));
            }
            if(loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
                loggerFactory.AddProvider(new DebugLoggerProvider());
            }
            _logger = loggerFactory.CreateLogger<MSMQServer>();
            _emiter = emiter;
            _inMemoryEventBus = inMemoryEventBus;
            _configuration = configuration;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Start the MSMQ in-app server.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default)
            {
                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;
            }
            else
            {
                _token = cancellationToken;
            }
            var queue = Helpers.GetMessageQueue(_configuration.QueueName);

            RunningTask = Task.Run(() =>
            {
                while (true)
                {
                    var message = queue.Receive();
                    //Start every reception in a separate task for perf and safety.
                    Task.Run(async () =>
                    {
                        if (!string.IsNullOrWhiteSpace(message.Body?.ToString()))
                        {
                            try
                            {
                                var enveloppe = message.Body.ToString().FromJson<Enveloppe>();
                                if (enveloppe.Emiter != _emiter)
                                {
                                    var data = enveloppe.Data.FromJson(Type.GetType(enveloppe.AssemblyQualifiedDataType));
                                    _configuration?.Callback(data);
                                    if (data is IDomainEvent @event && (_configuration == null || _configuration.DispatchInMemory))
                                    {
                                        await _inMemoryEventBus?.PublishEventAsync(@event);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _logger?.LogError($"MSMQServer : Cannot treat message id {message.Id} {Environment.NewLine} {e}");
                            }
                        }
                    }, cancellationToken);
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop the MSMQ server.
        /// </summary>
        public void Stop()
        {
            try
            {
                RunningTask.Dispose();
            }
            finally
            {
                RunningTask = null;
            }
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
            }
            else
            {
                throw new InvalidOperationException("MSMQServer.Stop() : Cancellation couldn't be properly requested, because you provide " +
                    "a cancellationToken to the start method. To stop the server, you should cancel this token." +
                    " However, running task has been cleaned from memory.");
            }
        }

        #endregion

        #region Overriden methods

        #endregion

    }


}