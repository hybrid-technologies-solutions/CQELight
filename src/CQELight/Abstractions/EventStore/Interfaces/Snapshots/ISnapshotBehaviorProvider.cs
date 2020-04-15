using System;

namespace CQELight.Abstractions.EventStore.Interfaces
{
    /// <summary>
    /// Defines a contract interface for snapshot behavior provider.
    /// </summary>
    public interface ISnapshotBehaviorProvider
    {
        /// <summary>
        /// Gets the behavior according of a specific event type.
        /// Please note that this can be called multiple times from concurrent thread, so
        /// you should pay attention to thread safety in your own implementation.
        /// </summary>
        /// <param name="type">Event type.</param>
        /// <returns>Snapshot behavior.</returns>
        ISnapshotBehavior? GetBehaviorForEventType(Type type);
    }
}
