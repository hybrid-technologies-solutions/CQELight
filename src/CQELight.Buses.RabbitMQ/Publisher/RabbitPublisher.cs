using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.Buses.RabbitMQ.Publisher
{
    /// <summary>
    /// Instance for publishing data to Rabbit instance.
    /// </summary>
    public class RabbitPublisher : IDomainEventBus, ICommandBus
    {
        #region Members

        private readonly ILogger<RabbitPublisher> logger;
        private readonly RabbitPublisherConfiguration configuration;

        #endregion

        #region Ctor

        public RabbitPublisher(
            ILoggerFactory loggerFactory,
            RabbitPublisherConfiguration configuration)
        {
            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
                loggerFactory.AddProvider(new DebugLoggerProvider());
            }
            logger = loggerFactory.CreateLogger<RabbitPublisher>();
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            RabbitCommonTools.DeclareExchangesAndQueueForPublisher(GetChannel(GetConnection()), configuration);
        }

        #endregion

        #region Public methods

        #region ICommandBus

        /// <summary>
        /// Dispatch command asynchrounously.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="context">Context associated to command.</param>
        /// <returns>List of launched tasks from handler.</returns>
        public Task<Result> DispatchAsync(ICommand command, ICommandContext? context = null)
        {
            if (command != null)
            {
                var commandType = command.GetType();
                logger.LogDebug(() => $"RabbitMQClientBus : Beginning of publishing command of type {commandType.FullName}");
                using (var connection = GetConnection())
                {
                    using (var channel = GetChannel(connection))
                    {
                        var env = GetEnveloppeForCommand(command);
                        var body = Encoding.UTF8.GetBytes(env.ToJson());
                        var props = GetBasicProperties(channel, env);

                        var routingKey = configuration.RoutingKeyFactory.GetRoutingKeyForCommand(command);

                        channel.BasicPublish(
                            exchange: "", //Command sending are direct
                            routingKey: routingKey,
                            basicProperties: props,
                            body: body);
                    }
                }
                logger.LogDebug(() => $"RabbitMQClientBus : End of publishing command of type {commandType.FullName}");
                return Result.Ok();
            }
            return Result.Fail("RabbitMQClientBus : No command provided to publish method");
        }

        #endregion

        #region IEventBus

        /// <summary>
        /// Publish asynchronously an event to be processed by the bus.
        /// </summary>
        /// <param name="event">Event to register.</param>
        /// <param name="context">Context associated to the event..</param>
        public async Task<Result> PublishEventAsync(IDomainEvent @event, IEventContext? context = null)
        {
            if (@event != null)
            {
                var eventType = @event.GetType();
                logger.LogDebug(() => $"RabbitMQClientBus : Beginning of publishing event of type {eventType.FullName}");
                var routingKey = configuration.RoutingKeyFactory.GetRoutingKeyForEvent(@event);
                await Publish(GetEnveloppeFromEvent(@event), routingKey).ConfigureAwait(false);
                logger.LogDebug(() => $"RabbitMQClientBus : End of publishing event of type {eventType.FullName}");
                return Result.Ok();
            }
            return Result.Fail("RabbitMQClientBus : No event provided to publish method");
        }

        /// <summary>
        /// Public asynchronously a bunch of events to be processed by the bus.
        /// </summary>
        /// <param name="events">Data that contains all events</param>
        public async Task<Result> PublishEventRangeAsync(IEnumerable<IDomainEvent> events)
        {
            logger.LogInformation(() => "RabbitMQClientBus : Beginning of treating bunch of events");
            var eventsGroup = events.GroupBy(d => d.GetType())
                .Select(g => new
                {
                    Type = g.Key,
                    Events = g.OrderBy(e => e.EventTime).ToList()
                }).ToList();

#pragma warning disable
            Task.Run(() =>
            {
                logger.LogDebug(() => $"RabbitMQClientBus : Found {eventsGroup.Count} group(s) :");
                eventsGroup.ForEach(e => logger.LogDebug(() => $"\t Event of type {e.Type} : {e.Events.Count} events"));
            });
#pragma warning restore

            var tasks = new List<Task<Result>>();
            foreach (var item in eventsGroup.Where(e => e.Events.Count > 0))
            {
                var routingKey = configuration.RoutingKeyFactory.GetRoutingKeyForEvent(item.Events[0]);
                tasks.Add(Task.Run(async () =>
                {
                    logger.LogInformation(() => $"RabbitMQClientBus : Beginning of parallel dispatching events of type {item.Type.FullName}");
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

                                    foreach (var cfg in configuration.NetworkInfos.ServiceExchangeDescriptions
                                        .Where(s => (s.ExchangeContentType & ExchangeContentType.Event) != 0))
                                    {
                                        batch.Add(exchange: cfg.ExchangeName,
                                                  routingKey: routingKey,
                                                  mandatory: true,
                                                  properties: props,
                                                  body: body);
                                    }
                                }
                                batch.Publish();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogErrorMultilines($"RabbitMQClientBus : Error when dispatching batch", e.ToString());
                        return Result.Fail();
                    }
                    return Result.Ok();
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return Result.Ok().Combine(tasks.Select(t => t.Result).ToArray());
        }

        #endregion

        #endregion

        #region Private methods

        private IBasicProperties GetBasicProperties(IModel channel, Enveloppe env)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            props.ContentType = "text/json";
            props.DeliveryMode = (byte)(env.PersistentMessage ? 2 : 1);
            props.Type = env.AssemblyQualifiedDataType;
            props.Expiration =
                env.Expiration.TotalMilliseconds > 0 ?
                (env.Expiration.TotalMilliseconds * 1000).ToString() :
                "3600000000";
            return props;
        }

        private IConnection GetConnection() => configuration.ConnectionInfos.ConnectionFactory.CreateConnection();

        private Enveloppe GetEnveloppeForCommand(ICommand command)
        {
            var commandType = command.GetType();
            var serializedCommand = configuration.Serializer.SerializeCommand(command);
            return new Enveloppe(serializedCommand, commandType, configuration.ConnectionInfos.ServiceName);
        }

        private Enveloppe GetEnveloppeFromEvent(IDomainEvent @event)
        {
            var eventType = @event.GetType();
            var evtCfg = configuration.EventsLifetime.FirstOrDefault(t => new TypeEqualityComparer().Equals(t.EventType, @event.GetType()));
            TimeSpan? expiration = null;
            if (evtCfg.LifeTime.TotalMilliseconds > 0)
            {
                expiration = evtCfg.LifeTime;
                logger.LogDebug(() => $"RabbitMQClientBus : Defining {evtCfg.LifeTime} lifetime for event of type {eventType.FullName}");
            }
            var serializedEvent = configuration.Serializer.SerializeEvent(@event);
            if (expiration.HasValue)
            {
                return new Enveloppe(serializedEvent, eventType, configuration.ConnectionInfos.ServiceName, true, expiration.Value);
            }
            return new Enveloppe(serializedEvent, eventType, configuration.ConnectionInfos.ServiceName);
        }

        private Task Publish(Enveloppe env, string routingKey)
        {
            using (var connection = GetConnection())
            {
                using (var channel = GetChannel(connection))
                {
                    var body = Encoding.UTF8.GetBytes(env.ToJson());
                    var props = GetBasicProperties(channel, env);

                    foreach (var cfg in configuration.NetworkInfos.ServiceExchangeDescriptions
                        .Where(s => (s.ExchangeContentType & ExchangeContentType.Event) != 0))
                    {
                        channel.BasicPublish(
                                             exchange: cfg.ExchangeName,
                                             routingKey: routingKey,
                                             basicProperties: props,
                                             body: body);
                    }
                }
            }
            return Task.CompletedTask;
        }

        private IModel GetChannel(IConnection connection) => connection.CreateModel();

        #endregion
    }
}
