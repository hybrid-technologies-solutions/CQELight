//using RabbitMQ.Client;
//using System.Text;
//using System.Threading.Tasks;
//using CQELight.Abstractions.Events.Interfaces;
//using CQELight.Tools.Extensions;
//using CQELight.Abstractions.Dispatcher;
//using System;
//using System.Linq;
//using CQELight.Tools;
//using CQELight.Buses.RabbitMQ.Extensions;
//using System.Collections.Generic;
//using Microsoft.Extensions.Logging;
//using CQELight.Abstractions.DDD;
//using Microsoft.Extensions.Logging.Debug;

//namespace CQELight.Buses.RabbitMQ.Publisher
//{
//    /// <summary>
//    /// RabbitMQ client bus instance. 
//    /// It uses its configuration to push to a RabbitMQ instance.
//    /// </summary>
//    public class RabbitMQEventBus : BaseRabbitMQPublisherBus, IDomainEventBus
//    {
//        #region Ctor

//        /// <summary>
//        /// Creates a new RabbitMQ Event bus.
//        /// </summary>
//        /// <param name="serializer">Object serializer.</param>
//        /// <param name="configuration">Configuration to use for using RabbitMQ</param>
//        /// <param name="loggerFactory">LoggerFactory</param>
//        public RabbitMQEventBus(
//            IDispatcherSerializer serializer,
//            RabbitPublisherBusConfiguration configuration,
//            ILoggerFactory loggerFactory = null)
//            : base(serializer, configuration, loggerFactory)
//        {
//        }

//        #endregion

//        #region IDomainEventBus

//        /// <summary>
//        /// Register asynchronously an event to be processed by the bus.
//        /// </summary>
//        /// <param name="event">Event to register.</param>
//        /// <param name="context">Context associated to the event.</param>
//        public async Task<Result> PublishEventAsync(IDomainEvent @event, IEventContext context = null)
//        {
//            if (@event != null)
//            {
//                var eventType = @event.GetType();
//                Logger.LogDebug($"RabbitMQClientBus : Beginning of publishing event of type {eventType.FullName}");
//                await Publish(GetEnveloppeFromEvent(@event)).ConfigureAwait(false);
//                Logger.LogDebug($"RabbitMQClientBus : End of publishing event of type {eventType.FullName}");
//                return Result.Ok();
//            }
//            return Result.Fail("RabbitMQClientBus : No event provided to publish method");
//        }

//        public async Task<Result> PublishEventRangeAsync(IEnumerable<IDomainEvent> events)
//        {
//            Logger.LogInformation("RabbitMQClientBus : Beginning of treating bunch of events");
//            var eventsGroup = events.GroupBy(d => d.GetType())
//                .Select(g => new
//                {
//                    Type = g.Key,
//                    Events = g.OrderBy(e => e.EventTime).ToList()
//                }).ToList();

//#pragma warning disable 
//            Task.Run(() =>
//            {
//                Logger.LogDebug($"RabbitMQClientBus : Found {eventsGroup.Count} group(s) :");
//                eventsGroup.ForEach(e => Logger.LogDebug($"\t Event of type {e.Type} : {e.Events.Count} events"));
//            });
//#pragma warning restore

//            var tasks = new List<Task<Result>>();
//            foreach (var item in eventsGroup)
//            {
//                var allowParallelDispatch = Configuration.ParallelDispatchEventTypes.Any(t => new TypeEqualityComparer().Equals(item.Type, t));
//                tasks.Add(Task.Run(async () =>
//                {
//                    if (allowParallelDispatch)
//                    {
//                        Logger.LogInformation($"RabbitMQClientBus : Beginning of parallel dispatching events of type {item.Type.FullName}");
//                        var innerTasks = new List<Task<Enveloppe>>();
//                        foreach (var @event in item.Events)
//                        {
//                            innerTasks.Add(Task.Run(() => GetEnveloppeFromEvent(@event)));
//                        }
//                        await Task.WhenAll(innerTasks).ConfigureAwait(false);
//                        var enveloppes = innerTasks.Select(e => e.Result);
//                        try
//                        {
//                            using (var connection = GetConnection())
//                            {
//                                using (var channel = GetChannel(connection))
//                                {
//                                    var batch = channel.CreateBasicPublishBatch();
//                                    foreach (var env in enveloppes)
//                                    {
//                                        var body = Encoding.UTF8.GetBytes(env.ToJson());
//                                        var props = GetBasicProperties(channel, env);

//                                        var configs = Configuration
//                                            .PublisherConfiguration
//                                            .EventsConfiguration
//                                            .Where(c => c.Types.Any(t => t.AssemblyQualifiedName == env.AssemblyQualifiedDataType));

//                                        foreach (var cfg in configs)
//                                        {
//                                            batch.Add(exchange: cfg.ExchangeName,
//                                                      routingKey: "",
//                                                      mandatory: true,
//                                                      properties: props,
//                                                      body: body);
//                                        }
//                                    }
//                                    batch.Publish();
//                                }
//                            }
//                        }
//                        catch (Exception e)
//                        {
//                            Logger.LogErrorMultilines($"RabbitMQClientBus : Error when dispatching batch", e.ToString());
//                            return Result.Fail();
//                        }
//                    }
//                    else
//                    {
//                        Logger.LogInformation($"RabbitMQClientBus : Beginning of single op dispatching events of type {item.Type.FullName}");
//                        foreach (var evtData in item.Events)
//                        {
//                            await PublishEventAsync(evtData).ConfigureAwait(false);
//                        }
//                    }
//                    return Result.Ok();
//                }));
//            }

//            await Task.WhenAll(tasks).ConfigureAwait(false);
//            return Result.Ok().Combine(tasks.Select(t => t.Result).ToArray());
//        }

//        #endregion

//        #region Private methods

//        private Enveloppe GetEnveloppeFromEvent(IDomainEvent @event)
//        {
//            var eventType = @event.GetType();
//            var evtCfg = Configuration.EventsLifetime.FirstOrDefault(t => new TypeEqualityComparer().Equals(t.EventType, @event.GetType()));
//            TimeSpan? expiration = null;
//            if (evtCfg.LifeTime.TotalMilliseconds > 0)
//            {
//                expiration = evtCfg.LifeTime;
//                Logger.LogDebug($"RabbitMQClientBus : Defining {evtCfg.LifeTime.ToString()} lifetime for event of type {eventType.FullName}");
//            }
//            var serializedEvent = Serializer.SerializeEvent(@event);
//            if (expiration.HasValue)
//            {
//                return new Enveloppe(serializedEvent, eventType, Configuration.Emiter, true, expiration.Value);
//            }
//            return new Enveloppe(serializedEvent, eventType, Configuration.Emiter);

//        }

//        private Task Publish(Enveloppe env)
//        {
//            using (var connection = GetConnection())
//            {
//                using (var channel = GetChannel(connection))
//                {
//                    var body = Encoding.UTF8.GetBytes(env.ToJson());
//                    var props = GetBasicProperties(channel, env);

//                    var configs = Configuration
//                                            .PublisherConfiguration
//                                            .EventsConfiguration
//                                            .Where(c => c.Types.Any(t => t.AssemblyQualifiedName == env.AssemblyQualifiedDataType));

//                    foreach (var cfg in configs)
//                    {
//                        channel.BasicPublish(
//                                             exchange: cfg.ExchangeName,
//                                             routingKey: "",
//                                             basicProperties: props,
//                                             body: body);
//                    }
//                }
//            }
//            return Task.CompletedTask;
//        }

//        private IModel GetChannel(IConnection connection)
//        {
//            var channel = connection.CreateModel();
//            var exchangeName = Configuration.Emiter + "_events";

//            channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true);

//            return channel;
//        }

//        #endregion

//    }
//}
