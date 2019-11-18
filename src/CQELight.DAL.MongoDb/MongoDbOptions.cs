using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.DAL.MongoDb
{
    /// <summary>
    /// Options of MongoDb connection.
    /// </summary>
    public class MongoDbOptions
    {
        #region Properties

        /// <summary>
        /// URL to connect to Mongo server.
        /// </summary>
        public MongoUrl Url { get; private set; }

        /// <summary>
        /// Collection of custom serializers
        /// </summary>
        public IEnumerable<IBsonSerializer> CustomSerializers { get; set; }

        /// <summary>
        /// Name of used database.
        /// If not set by user, "DefaultDatabase" is used
        /// </summary>
        public string DatabaseName { get; private set; } = "DefaultDatabase";

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new <see cref="MongoDbOptions"/> instance with no authentication and default database.
        /// </summary>
        /// <param name="serversUrls">Server urls to connect to</param>
        [Obsolete("Use ctor with MongoServerAddress")]
        public MongoDbOptions(params string[] serversUrls)
            : this(new MongoUrlBuilder
            {
                Servers = serversUrls.Select(u => new MongoServerAddress(u))
            }.ToMongoUrl())
        {

        }

        /// <summary>
        /// Initializes a new <see cref="MongoDbOptions"/> instance with no authentication and default database.
        /// </summary>
        /// <param name="serversUrls">Server urls to connect to</param>
        public MongoDbOptions(params MongoServerAddress[] serversUrls)
            : this(new MongoUrlBuilder
            {
                Servers = serversUrls
            }.ToMongoUrl())
        {

        }

        /// <summary>
        /// Initializes a new <see cref="MongoDbOptions"/> instances with authentication using default database.
        /// </summary>
        /// <param name="username">Username to access Mongo server</param>
        /// <param name="password">Password to access Mongo server</param>
        /// <param name="serversUrls">Server urls to connect to</param>
        [Obsolete("Use ctor with MongoServerAddress")]
        public MongoDbOptions(string username, string password, params string[] serversUrls)
            : this(new MongoUrlBuilder
            {
                Servers = serversUrls.Select(u => new MongoServerAddress(u)),
                Username = username,
                Password = password
            }.ToMongoUrl())
        {

        }

        /// <summary>
        /// Initializes a new <see cref="MongoDbOptions"/> instances with authentication using default database.
        /// </summary>
        /// <param name="username">Username to access Mongo server</param>
        /// <param name="password">Password to access Mongo server</param>
        /// <param name="serversUrls">Server urls to connect to</param>
        public MongoDbOptions(string username, string password, params MongoServerAddress[] serversUrls)
            : this(new MongoUrlBuilder
            {
                Servers = serversUrls,
                Username = username,
                Password = password
            }.ToMongoUrl())
        {

        }

        /// <summary>
        /// Initializes a new <see cref="MongoDbOptions"/> instances with authentication using specified database.
        /// </summary>
        /// <param name="username">Username to access Mongo server</param>
        /// <param name="password">Password to access Mongo server</param>
        /// <param name="database">Database to use.</param>
        /// <param name="serversUrls">Server urls to connect to</param>
        public MongoDbOptions(string username, string password, string database, params MongoServerAddress[] serversUrls)
            : this(new MongoUrlBuilder
            {
                Servers = serversUrls,
                Username = username,
                Password = password
            }.ToMongoUrl())
        {
            DatabaseName = database;
        }

        /// <summary>
        /// Initializes a new <see cref="MongoDbOptions"/> instances with fully qualified URL.
        /// </summary>
        /// <param name="url">URL to connect to Mongo</param>
        public MongoDbOptions(MongoUrl url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            if (!string.IsNullOrWhiteSpace(Url.DatabaseName))
            {
                DatabaseName = Url.DatabaseName;
            }
        }

        #endregion

    }
}
