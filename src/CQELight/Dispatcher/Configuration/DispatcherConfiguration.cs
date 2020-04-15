using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Dispatcher.Configuration.Internal;
using CQELight.Events.Serializers;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.Dispatcher.Configuration
{
    /// <summary>
    /// Configuration instance to use for the dispatcher.
    /// </summary>
    public class DispatcherConfiguration
    {
        #region Members

        private static DispatcherConfiguration? _default;
        private static DispatcherConfiguration? _current;
        private readonly bool _strict;

        #endregion

        #region Properties

        internal IEnumerable<EventDispatchConfiguration> EventDispatchersConfiguration { get; set; }
            = new List<EventDispatchConfiguration>();
        internal IEnumerable<CommandDispatchConfiguration> CommandDispatchersConfiguration { get; set; }
            = new List<CommandDispatchConfiguration>();

        #endregion

        #region Public static members

        /// <summary>
        /// Default instance of the configuration.
        /// This default configuration map every events within the system with all available bus, use Json as default serializer,
        /// simply ignores errors.
        /// </summary>
        public static DispatcherConfiguration Default
        {
            get
            {
                if (_default == null)
                {
                    var builder = new DispatcherConfigurationBuilder();
                    builder.ForAllEvents().UseAllAvailableBuses().SerializeWith<JsonDispatcherSerializer>();
                    builder.ForAllCommands().UseAllAvailableBuses().SerializeWith<JsonDispatcherSerializer>();
                    _default = builder.Build();
                }
                return _default;
            }
        }

        /// <summary>
        /// Current instance of the configuration.
        /// If not specified, accessing it will use Default.
        /// </summary>
        public static DispatcherConfiguration Current
        {
            get
            {
                return _current ?? Default;
            }
            internal set
            {
                _current = value;
            }
        }

        #endregion

        #region Ctor

        internal DispatcherConfiguration(bool strict)
        {
            _strict = strict;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// <para>
        /// Do a strict validation upon the configuration.
        /// It means that every events need to be dispatched in at least one bus. 
        /// </para>
        /// <para>
        /// If the configuration was not build with the strict flag, this will returns true in all cases.
        /// </para>
        /// </summary>
        /// <returns>True if the configuration is stricly valid, false otherwise.</returns>
        public bool ValidateStrict()
        {
            if (_strict)
            {
                var typeComparer = new TypeEqualityComparer();
                var allTypes = ReflectionTools.GetAllTypes().Where(t =>
                    (typeof(IDomainEvent).IsAssignableFrom(t) || typeof(ICommand).IsAssignableFrom(t)) && t.IsClass).ToList();
                return allTypes.All(t =>
                            EventDispatchersConfiguration.Any(cfg => cfg.BusesTypes.WhereNotNull().Any()));
            }
            return true;
        }

        #endregion

        #region Overriden methods

        /// <summary>
        /// Express the whole configuration as string for debug purposes.
        /// </summary>
        /// <returns>Configuration as string.</returns>
        public override string ToString()
        {
            StringBuilder config = new StringBuilder();
            foreach (var configData in EventDispatchersConfiguration)
            {
                config.Append($"Event of type {configData.EventType.FullName} : ");

                config.AppendLine($"Error handler defined ? {(configData.ErrorHandler != null ? "yes" : "no")}");
                config.AppendLine($"Serialize events with : {configData.Serializer?.GetType().FullName}");
                foreach (var dispatchData in configData.BusesTypes)
                {
                    try
                    {
                        config.AppendLine($" -> Dispatch activated on bus {dispatchData.FullName}");
                    }
                    catch
                    {
                        //Exception ignored because no need to handle it when expressing if it happens
                    }
                }
            }
            return config.ToString();
        }

        #endregion

    }
}
