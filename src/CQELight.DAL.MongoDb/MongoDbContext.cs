using MongoDB.Driver;

namespace CQELight.DAL.MongoDb
{
    /// <summary>
    /// A specific Db Context related to MongoDB.
    /// </summary>
    public static class MongoDbContext
    {
        #region Properties

        /// <summary>
        /// Current instance of the MongoDB Client.
        /// Only configured after Bootstrapper.Boostrapp is called.
        /// </summary>
        public static MongoClient MongoClient { get; internal set; } = default!;
        internal static IMongoDatabase Database => MongoClient.GetDatabase(DatabaseName ?? "DefaultDatabase");
        internal static string? DatabaseName { get; set; } = null;

        #endregion

    }
}
