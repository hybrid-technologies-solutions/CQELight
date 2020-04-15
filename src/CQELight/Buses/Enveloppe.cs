using CQELight.Tools.Extensions;
using System;

namespace CQELight.Buses
{
    /// <summary>
    /// Base class to hold data for sending messages to third party message queues.
    /// </summary>
    public class Enveloppe
    {
        #region Properties

#pragma warning disable RCS1170 // Use read-only auto-implemented property.
        /// <summary>
        /// Serialized data of the message.
        /// </summary>
        public string Data { get; private set; } = default!;
        /// <summary>
        /// Assembly qualified type of the serialized data.
        /// </summary>
        public string AssemblyQualifiedDataType { get; private set; } = default!;
        /// <summary>
        /// Flag that indicates if the message is persistent or not.
        /// </summary>
        public bool PersistentMessage { get; private set; }
        /// <summary>
        /// Expiration time span to let know message queue system when a message is ready for
        /// deletion.
        /// </summary>
        public TimeSpan Expiration { get; private set; }
        /// <summary>
        /// Unique ID/Name of the emiter
        /// </summary>
        public string Emiter { get; private set; } = default!;
#pragma warning restore RCS1170 // Use read-only auto-implemented property.

        #endregion

        #region Ctor

        internal Enveloppe() { }

        /// <summary>
        /// Create a new enveloppe that contains data,
        /// which is persistent and have one day expiration.
        /// </summary>
        /// <param name="data">Serialized data object to be inserted into the enveloppe.</param>
        /// <param name="dataType">Type of serialized data</param>
        /// <param name="emiter">Unique id/name of the emiter</param>
        public Enveloppe(string data, Type dataType, string emiter)
            : this(data, dataType, emiter, true, TimeSpan.FromDays(1))
        { }

        /// <summary>
        /// Create a new enveloppe that contains data,
        /// with a defined expiration.
        /// </summary>
        /// <param name="data">Serialized data object to be inserted into the enveloppe.</param>
        /// <param name="dataType">Type of serialized data</param>
        /// <param name="emiter">Unique id/name of the emiter</param>
        /// <param name="expiration">Time before message expires.</param>
        public Enveloppe(string data, Type dataType, string emiter, TimeSpan expiration)
            : this(data, dataType, emiter, true, expiration)
        { }

        /// <summary>
        /// Create a new enveloppe that contains data,
        /// which have one day expiration.
        /// when any
        /// </summary>
        /// <param name="data">Serialized data object to be inserted into the enveloppe.</param>
        /// <param name="dataType">Type of serialized data</param>
        /// <param name="emiter">Unique id/name of the emiter</param>
        /// <param name="persistent">Persistent flag, that indicates if message should be
        /// automatically deleted when anyone ack it.</param>
        public Enveloppe(string data, Type dataType, string emiter, bool persistent)
            : this(data, dataType, emiter, persistent, TimeSpan.FromDays(1))
        { }

        /// <summary>
        /// Create a new enveloppe with defined informations.
        /// </summary>
        /// <param name="data">Serialized data object to be inserted into the enveloppe.</param>
        /// <param name="dataType">Type of serialized data</param>
        /// <param name="emiter">Unique id/name of the emiter</param>
        /// <param name="expiration">Time before message expires.</param>
        /// <param name="persistent">Persistent flag, that indicates if message should be
        /// automatically deleted when anyone ack it.</param>
        public Enveloppe(string data, Type dataType, string emiter, bool persistent, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentNullException(nameof(data));
            }
            Data = data;
            AssemblyQualifiedDataType = dataType.AssemblyQualifiedName;
            PersistentMessage = persistent;
            Expiration = expiration;
            Emiter = emiter;
        }

        /// <summary>
        /// Create a new enveloppe with a defined object and all informations.
        /// </summary>
        /// <param name="value">Object to carry in enveloppe. Will be serialized in JSON.</param>
        /// <param name="emiter">Emiter of the message.</param>
        /// <param name="persistent">Persistent flag, that indicates if message should be
        /// automatically deleted when anyone ack it.</param>
        /// <param name="expiration">Time before message expires.</param>
        public Enveloppe(object value, string emiter, bool persistent, TimeSpan expiration)
            :this(value.ToJson(), value.GetType(), emiter, persistent, expiration)
        {
        }

        /// <summary>
        /// Create a new enveloppe with a defined object
        /// </summary>
        /// <param name="value">Object to carry in enveloppe. Will be serialized in JSON.</param>
        /// <param name="emiter">Emiter of the message.</param>
        public Enveloppe(object value, string emiter)
            : this(value.ToJson(), value.GetType(), emiter)
        {
        }

        /// <summary>
        /// Create a new enveloppe with a defined object and persistent information.
        /// </summary>
        /// <param name="value">Object to carry in enveloppe. Will be serialized in JSON.</param>
        /// <param name="emiter">Emiter of the messagE.</param>
        /// <param name="persistent">Persistent flag, that indicates if message should be
        /// automatically deleted when anyone ack it.</param>
        public Enveloppe(object value, string emiter, bool persistent)
            : this (value.ToJson(), value.GetType(), emiter, persistent)
        { }

        #endregion

    }
}
