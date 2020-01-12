﻿using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.DAL.MongoDb
{
    internal static class MongoDbContext
    {
        #region Properties

        public static MongoClient MongoClient { get; set; }
        public static IMongoDatabase Database => MongoClient.GetDatabase(DatabaseName ?? "DefaultDatabase");
        public static string DatabaseName { get; set; } = null;
        public static bool SupportNativeSession => MongoClient.Settings.Servers?.Count() > 1;

        #endregion

    }
}
