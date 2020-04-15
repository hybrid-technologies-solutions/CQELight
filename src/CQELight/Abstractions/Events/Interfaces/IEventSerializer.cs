using System;

namespace CQELight.Abstractions.Events.Interfaces
{
    /// <summary>
    /// Contract interface for event serializer.
    /// </summary>
    public interface IEventSerializer
    {
        /// <summary>
        /// Serialize the event into a string.
        /// </summary>
        /// <param name="event">Event to serialize.</param>
        /// <returns>Event that has been seriazlied into a string.</returns>
        string SerializeEvent(IDomainEvent @event);
        /// <summary>
        /// Deserialize event from string.
        /// </summary>
        /// <param name="data">String data that contains event.</param>
        /// <param name="type">Type of event object.</param>
        /// <returns>Instance of event.s</returns>
        IDomainEvent? DeserializeEvent(string data, Type type);
        /// <summary>
        /// Deserialize event from string.
        /// </summary>
        /// <param name="data">String data that contains event.</param>
        /// <typeparam name="T">Type of domain event to obtain.</typeparam>
        /// <returns>Instance of event.s</returns>
        T DeserializeEvent<T>(string data)
            where T : IDomainEvent;
    }
}
