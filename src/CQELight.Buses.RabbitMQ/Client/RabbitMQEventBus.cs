using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Tools.Extensions;
using CQELight.Abstractions.Dispatcher;
using System;
using System.Linq;
using CQELight.Tools;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using CQELight.Abstractions.DDD;
using Microsoft.Extensions.Logging.Debug;

namespace CQELight.Buses.RabbitMQ.Client
{
    /// <summary>
    /// RabbitMQ client bus instance.
    /// It uses its configuration to push to a RabbitMQ instance.
    /// </summary>
    [Obsolete("Use CQELight.Buses.RabbitMQ.Publisher.BaseRabbitMQPublisherBus instead")]
    public class RabbitMQEventBus : IDomainEventBus
    {
        #region Members

        private static RabbitPublisherBusConfiguration _configuration = RabbitPublisherBusConfiguration.Default;
        private readonly IDispatcherSerializer _serializer;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new RabbitMQ Event bus.
        /// </summary>
        /// <param name="serializer">Object serializer.</param>
        /// <param name="configuration">Configuration to use for using RabbitMQ</param>
        /// <param name="loggerFactory">LoggerFactory</param>
        public RabbitMQEventBus(
            IDispatcherSerializer serializer,
            RabbitPublisherBusConfiguration configuration,
            ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? new LoggerFactory(new[] { new DebugLoggerProvider() })).CreateLogger<RabbitMQEventBus>();
            _configuration = configuration ?? RabbitPublisherBusConfiguration.Default;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        #endregion

        #region IDomainEventBus

        /// <summary>
        /// Register asynchronously an event to be processed by the bus.
        /// </summary>
        /// <param name="event">Event to register.</param>
        /// <param name="context">Context associated to the event.</param>
        public async Task<Result> PublishEventAsync(IDomainEvent @event, IEventContext? context = null)
        {
            if (@event != null)
            {
                var eventType = @event.GetType();
                _logger.LogDebug(() => $"RabbitMQClientBus : Beginning of publishing event of type {eventType.FullName}");
                await Publish(GetEnveloppeFromEvent(@event)).ConfigureAwait(false);
                _logger.LogDebug(() => $"RabbitMQClientBus : End of publishing event of type {eventType.FullName}");
                return Result.Ok();
            }
            return Result.Fail("RabbitMQClientBus : No event provided to publish method");
        }

        public async Task<Result> PublishEventRangeAsync(IEnumerable<IDomainEvent> events)
        {
            _logger.LogInformation(() => $"RabbitMQClientBus : Beginning of treating bunch of events");
            var eventsGroup = events.GroupBy(d => d.GetType())
                .Select(g => new
                {
                    Type = g.Key,
                    Events = g.OrderBy(e => e.EventTime).ToList()
                }).ToList();

#pragma warning disable 
            Task.Run(() =>
            {
                _logger.LogDebug(() => $"RabbitMQClientBus : Found {eventsGroup.Count} group(s) :");
                eventsGroup.ForEach(e => _logger.LogDebug(() => $"\t Event of type {e.Type} : {e.Events.Count} events"));
            });
#pragma warning restore

            var tasks = new List<Task<Result>>();
            foreach (var item in eventsGroup)
            {
                var allowParallelDispatch = _configuration.ParallelDispatchEventTypes.Any(t => new TypeEqualityComparer().Equals(item.Type, t));
                tasks.Add(Task.Run(async () =>
                {
                    if (allowParallelDispatch)
                    {
                        _logger.LogInformation(() => $"RabbitMQClientBus : Beginning of parallel dispatching events of type {item.Type.FullName}");
                        var innerTasks = new List<Task<Enveloppe>>();
                        foreach (var @event in item.Events)
                        {
                            innerTasks.Add(Task.Run(() => GetEnveloppeFromEvent(@event)));
                        }
                        await Task.WhenAll(innerTasks).ConfigureAwait(false);
                        var enveloppes = innerTasks.Select(e => e.Result);
                        try
                        {
                            using (var connection = GetConnection())
                            {
                                using (var channel = GetChannel(connection))
                                {
                                    var batch = channel.CreateBasicPublishBatch();
                                    foreach (var env in enveloppes)
                                    {
                                        var body = Encoding.UTF8.GetBytes(env.ToJson());
                                        var props = GetBasicProperties(channel, env);
                                        batch.Add(exchange: Consts.CONST_CQE_EXCHANGE_NAME,
                                                         routingKey: "cqelight.events.*",
                                                         mandatory: true,
                                                         properties: props,
                                                         body: body);
                                    }
                                    batch.Publish();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogErrorMultilines($"RabbitMQClientBus : Error when dispatching batch", e.ToString());
                            return Result.Fail();
                        }
                    }
                    else
                    {
                        _logger.LogInformation(() => $"RabbitMQClientBus : Beginning of single op dispatching events of type {item.Type.FullName}");
                        foreach (var evtData in item.Events)
                        {
                            await PublishEventAsync(evtData).ConfigureAwait(false);
                        }
                    }
                    return Result.Ok();
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return Result.Ok().Combine(tasks.Select(t => t.Result).ToArray());
        }

        #endregion

        #region Private methods

        private Enveloppe GetEnveloppeFromEvent(IDomainEvent @event)
        {
            var eventType = @event.GetType();
            var evtCfg = _configuration.EventsLifetime.FirstOrDefault(t => new TypeEqualityComparer().Equals(t.EventType, @event.GetType()));
            TimeSpan? expiration = null;
            if (evtCfg.LifeTime.TotalMilliseconds > 0)
            {
                expiration = evtCfg.LifeTime;
                _logger.LogDebug(() => $"RabbitMQClientBus : Defining {evtCfg.LifeTime} lifetime for event of type {eventType.FullName}");
            }
            var serializedEvent = _serializer.SerializeEvent(@event);
            if (expiration.HasValue)
            {
                return new Enveloppe(serializedEvent, eventType, _configuration.Emiter, true, expiration.Value);
            }
            return new Enveloppe(serializedEvent, eventType, _configuration.Emiter);
        }

        private IBasicProperties GetBasicProperties(IModel channel, Enveloppe env)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            props.ContentType = "text/json";
            props.DeliveryMode = (byte)(env.PersistentMessage ? 2 : 1);
            props.Type = env.AssemblyQualifiedDataType;
            return props;
        }

        private Task Publish(Enveloppe env)
        {
            using (var connection = GetConnection())
            {
                using (var channel = GetChannel(connection))
                {
                    var body = Encoding.UTF8.GetBytes(env.ToJson());
                    var props = GetBasicProperties(channel, env);
                    channel.BasicPublish(
                                         exchange: _configuration.Emiter + "_events",
                                         routingKey: "",
                                         basicProperties: props,
                                         body: body);
                }
            }
            return Task.CompletedTask;
        }

        private IConnection GetConnection() => _configuration.ConnectionFactory.CreateConnection();

        private IModel GetChannel(IConnection connection)
        {
            var channel = connection.CreateModel();
            var exchangeName = _configuration.Emiter + "_events";

            channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true);

            return channel;
        }

        #endregion

    }
}
