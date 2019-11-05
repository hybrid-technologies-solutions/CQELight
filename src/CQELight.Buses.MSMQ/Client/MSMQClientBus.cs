using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.MSMQ.Common;
using CQELight.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading.Tasks;

namespace CQELight.Buses.MSMQ.Client
{
    /// <summary>
    /// MSMQ client bus instance. It uses specific configuration to push on a 
    /// MSMQ instance.
    /// </summary>
    [Obsolete("MSMQ extension is no more supported and will be removed in V2")]
    public class MSMQClientBus : IDomainEventBus
    {

        #region Members

        private static MSMQClientBusConfiguration _configuration;
        private readonly IDispatcherSerializer _serializer;
        private readonly string _emiter;

        #endregion

        #region Ctor

        internal MSMQClientBus(string emiter, IDispatcherSerializer serializer, MSMQClientBusConfiguration configuration = null)
        {
            if (string.IsNullOrWhiteSpace(emiter))
            {
                throw new ArgumentNullException(nameof(emiter));
            }
            _configuration = configuration ?? MSMQClientBusConfiguration.Default;
            _serializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
            _emiter = emiter;
        }

        #endregion

        #region IDomainEventBus

        /// <summary>
        /// Register asynchronously an event to be processed by the bus.
        /// </summary>
        /// <param name="event">Event to register.</param>
        /// <param name="context">Context associated to the event..</param>
        public Task<Result> PublishEventAsync(IDomainEvent @event, IEventContext context = null)
        {
            try
            {
                if (@event != null)
                {
                    var queue = Helpers.GetMessageQueue(_configuration.QueueName);

                    var evtCfg = _configuration.EventsLifetime.FirstOrDefault(t => new TypeEqualityComparer().Equals(t.Type, @event.GetType()));
                    TimeSpan? expiration = null;
                    if (evtCfg.Expiration.TotalMilliseconds > 0)
                    {
                        expiration = evtCfg.Expiration;
                    }

                    var serializedEvent = _serializer.SerializeEvent(@event);
                    var enveloppe = expiration.HasValue
                        ? new Enveloppe(serializedEvent, @event.GetType(), _emiter, true, expiration.Value)
                        : new Enveloppe(serializedEvent, @event.GetType(), _emiter);

                    var message = new Message(enveloppe)
                    {
                        TimeToBeReceived = enveloppe.Expiration,
                        Formatter = new JsonMessageFormatter()
                    };
                    queue.Send(message);
                }
                return Task.FromResult(Result.Ok());
            }
            catch
            {
                return Task.FromResult(Result.Fail());
            }
        }

        public Task<Result> PublishEventRangeAsync(IEnumerable<IDomainEvent> events)
        {
            var tasks = events.Select(d => PublishEventAsync(d)).ToList();
            Task.WhenAll(tasks);
            return Task.FromResult(Result.Ok().Combine(tasks.Select(t => t.Result).ToArray()));
        }

        #endregion

    }
}
