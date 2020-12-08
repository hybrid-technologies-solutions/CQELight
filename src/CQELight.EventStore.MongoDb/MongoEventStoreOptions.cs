using CQELight.Abstractions.EventStore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.EventStore.MongoDb
{
    /// <summary>
    /// Options for MongoDb
    /// </summary>
    public class MongoEventStoreOptions
    {
        #region Properties

        /// <summary>
        /// Collection of URLs to use as servers.
        /// </summary>
        public IEnumerable<string> ServerUrls { get; }
        /// <summary>
        /// Provider of snapshot behavior.
        /// </summary>
        public ISnapshotBehaviorProvider? SnapshotBehaviorProvider { get; }
        /// <summary>
        /// Behavior to follow with events that have been used when generating a snapshot.
        /// </summary>
        public SnapshotEventsArchiveBehavior SnapshotEventsArchiveBehavior { get; }
        /// <summary>
        /// Username to access database
        /// </summary>
        public string? Username { get; internal set; }
        /// <summary>
        /// Password to access databases
        /// </summary>
        public string? Password { get; internal set; }
        /// <summary>
        /// Flag that defines if non aggregate events should be persisted.
        /// </summary>
        public bool ShouldPersistNonAggregateEvent { get; set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new options class with a defined snapshot behavior provider and servers urls.
        /// </summary>
        /// <param name="snapshotBehaviorProvider">Snapshot behavior provider.</param>
        /// <param name="snapshotEventsArchiveBehavior">Behavior for archive events after creating snapshot.</param>
        /// <param name="serversUrls">Collection of urls to use as server.</param>
        public MongoEventStoreOptions(
            ISnapshotBehaviorProvider? snapshotBehaviorProvider,
            SnapshotEventsArchiveBehavior snapshotEventsArchiveBehavior = SnapshotEventsArchiveBehavior.StoreToNewDatabase,
            params string[] serversUrls)
        {
            if (serversUrls == null)
            {
                throw new ArgumentNullException(nameof(serversUrls));
            }
            if (serversUrls.Length == 0)
            {
                throw new ArgumentException("MongoDbEventStoreBootstrapperConfiguration.ctor() : At least one url should be provided, for main server.", nameof(serversUrls));
            }
            ServerUrls = serversUrls.AsEnumerable();
            SnapshotBehaviorProvider = snapshotBehaviorProvider;
            SnapshotEventsArchiveBehavior = snapshotEventsArchiveBehavior;
        }

        /// <summary>
        /// Creates a new options class with a defined snapshot behavior provider and servers urls.
        /// </summary>
        /// <param name="snapshotBehaviorProvider">Snapshot behavior provider.</param>
        /// <param name="snapshotEventsArchiveBehavior">Behavior for archive events after creating snapshot.</param>
        /// <param name="serversUrls">Collection of urls to use as server.</param>
        public MongoEventStoreOptions(
            string userName,
            string password,
            ISnapshotBehaviorProvider? snapshotBehaviorProvider,
            SnapshotEventsArchiveBehavior snapshotEventsArchiveBehavior = SnapshotEventsArchiveBehavior.StoreToNewDatabase,
            params string[] serversUrls) : this(snapshotBehaviorProvider, snapshotEventsArchiveBehavior, serversUrls)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("MongoDbEventStoreBootstrapperConfiguration.ctor() : Username should be provided", nameof(userName));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("MongoDbEventStoreBootstrapperConfiguration.ctor() : Password should be provided", nameof(password));
            }
            Username = userName;
            Password = password;
        }

        /// <summary>
        /// Create a new options class with following server urls.
        /// </summary>
        /// <param name="serversUrls">Collection of urls to use as server.</param>
        public MongoEventStoreOptions(string userName, string password, params string[] serversUrls)
            : this(userName, password, null, serversUrls: serversUrls)
        {
        }

        /// <summary>
        /// Create a new options class with following server urls.
        /// </summary>
        /// <param name="serversUrls">Collection of urls to use as server.</param>
        public MongoEventStoreOptions(params string[] serversUrls)
            : this(null, serversUrls: serversUrls)
        {
        }

        #endregion

    }
}
