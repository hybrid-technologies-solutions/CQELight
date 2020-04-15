using RabbitMQ.Client;
using System;

namespace CQELight.Buses.RabbitMQ
{
    /// <summary>
    /// Instance of RabbitMQClient.
    /// Use it to do custom advanced scenario when you need to call
    /// RabbitMQ directly.
    /// </summary>
    public class RabbitMQClient
    {
        #region Members

        private readonly AbstractBaseConfiguration _configuration;
        private static readonly object s_threadSafety = new object();
        private static RabbitMQClient? s_instance;

        #endregion

        #region Properties

        /// <summary>
        /// Access RabbitMQClient instance.
        /// Note : accessing this singleton instance is NOT recommended if you use
        /// IoC. You should inject it in your constructor to avoid issues.
        /// </summary>
        public static RabbitMQClient Instance
        {
            get
            {
                if (RabbitMQContext.Configuration == null)
                {
                    throw new InvalidOperationException("RabbitMQClient.Instance : Configuration hasn't been set before trying to access to RabbitClient");
                }
                if (s_instance == null)
                {
                    lock (s_threadSafety)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new RabbitMQClient(RabbitMQContext.Configuration);
                        }
                    }
                }
                return s_instance;
            }
        }

        #endregion

        #region Ctor

        internal RabbitMQClient(AbstractBaseConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves a new connection to RabbitMQ server, according to current configuration.
        /// </summary>
        /// <returns>RabbitMQ connection</returns>
        public IConnection GetConnection() => _configuration.ConnectionFactory.CreateConnection();

        #endregion
    }
}
