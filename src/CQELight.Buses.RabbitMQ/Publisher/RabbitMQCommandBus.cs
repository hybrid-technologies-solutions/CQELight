//using CQELight.Abstractions.CQS.Interfaces;
//using CQELight.Abstractions.DDD;
//using CQELight.Abstractions.Dispatcher;
//using CQELight.Tools;
//using CQELight.Tools.Extensions;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Debug;
//using RabbitMQ.Client;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CQELight.Buses.RabbitMQ.Publisher
//{
//    /// <summary>
//    /// RabbitMQ command bus implementation.
//    /// </summary>
//    public class RabbitMQCommandBus : BaseRabbitMQPublisherBus, ICommandBus
//    {
//        #region Members


//        #endregion

//        #region Ctor

//        /// <summary>
//        /// Creates a new RabbitMQ Command bus to interact with RabbitMQ for commands.
//        /// </summary>
//        /// <param name="serializer">Serializer to use for serializing commands.</param>
//        /// <param name="configuration">Publisher configuration.</param>
//        /// <param name="loggerFactory">LoggerFactory</param>
//        public RabbitMQCommandBus(
//            IDispatcherSerializer serializer,
//            RabbitPublisherBusConfiguration configuration,
//            ILoggerFactory loggerFactory = null)
//            : base(serializer, configuration, loggerFactory)
//        {
//        }

//        #endregion

//        #region ICommand bus methods

//        /// <summary>
//        /// Dispatch command asynchrounously.
//        /// </summary>
//        /// <param name="command">Command to dispatch.</param>
//        /// <param name="context">Context associated to command.</param>
//        /// <returns>List of launched tasks from handler.</returns>
//        public async Task<Result> DispatchAsync(ICommand command, ICommandContext context = null)
//        {
//            if (command != null)
//            {
//                var commandType = command.GetType();
//                Logger.LogDebug($"RabbitMQClientBus : Beginning of publishing command of type {commandType.FullName}");
//                await Publish(GetEnveloppeForCommand(command)).ConfigureAwait(false);
//                Logger.LogDebug($"RabbitMQClientBus : End of publishing command of type {commandType.FullName}");
//                return Result.Ok();
//            }
//            return Result.Fail("RabbitMQClientBus : No command provided to publish method");
//        }

//        #endregion

//        #region Private methods

//        private Enveloppe GetEnveloppeForCommand(ICommand command)
//        {
//            var commandType = command.GetType();
//            var serializedCommand = Serializer.SerializeCommand(command);
//            return new Enveloppe(serializedCommand, commandType, Configuration.Emiter);
//        }

//        private Task Publish(Enveloppe env)
//        {
//            using (var connection = GetConnection())
//            {
//                using (var channel = GetChannel(connection))
//                {
//                    var body = Encoding.UTF8.GetBytes(env.ToJson());
//                    var props = GetBasicProperties(channel, env);

//                    var commandConfg = Configuration
//                        .PublisherConfiguration
//                        .CommandsConfiguration
//                        .Where(c => c.Types.Any(t => t.AssemblyQualifiedName == env.AssemblyQualifiedDataType));

//                    foreach (var conf in commandConfg)
//                    {
//                        channel.BasicPublish(
//                                             exchange: conf.ExchangeName,
//                                             routingKey: env.AssemblyQualifiedDataType.Split('.')[0],
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
//            var exchangeName = Configuration.Emiter + "_commands";

//            channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, true);

//            return channel;
//        }

//        #endregion
//    }
//}
