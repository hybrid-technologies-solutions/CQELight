﻿using CQELight.EventStore.EFCore.Common;
using CQELight.IoC;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.EventStore.EFCore
{
    public static class BootstrapperExt
    {

        #region Extension methods

        /// <summary>
        /// Use SQLServer as EventStore for system, with the provided connection string.
        /// This is a usable case for common cases, in system that not have a huge amount of events, or for test/debug purpose.
        /// This is not recommanded for real-world big applications case.
        /// </summary>
        /// <param name="bootstrapper">Bootstrapper instance.</param>
        /// <param name="connectionString">Connection string to SQL Server.</param>
        /// <returns>Bootstrapper instance</returns>
        public static Bootstrapper UseSQLServerWithEFCoreAsEventStore(this Bootstrapper bootstrapper, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Bootstrapper.UseSQLServerWithEFCoreAsEventStore() : Connection string must be provided.", nameof(connectionString));
            }
            var service = new EFEventStoreBootstrappService
            {
                BootstrappAction = () =>
                {
                    AddDbContextRegistration(bootstrapper, connectionString);
                    EventStoreManager.Activate();
                }
            };
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        /// <summary>
        /// Use SQLite as EventStore for system, with the provided connection string.
        /// This is only recommanded for cases where SQLite is the only usable solution, such as mobile cases.
        /// </summary>
        /// <param name="bootstrapper">Bootstrapper instance.</param>
        /// <param name="connectionString">Connection string to SQLite.</param>
        /// <returns>Bootstrapper instance</returns>
        public static Bootstrapper UseSQLiteWithEFCoreAsEventStore(this Bootstrapper bootstrapper, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Bootstrapper.UseSQLiteWithEFCoreAsEventStore() : Connection string should be provided.", nameof(connectionString));
            }
            var service = new EFEventStoreBootstrappService
            {
                BootstrappAction = () =>
                {
                    AddDbContextRegistration(bootstrapper, connectionString, false);
                    EventStoreManager.Activate();
                }
            };
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        #endregion

        #region Private methods

        private static void AddDbContextRegistration(Bootstrapper bootstrapper, string connectionString, bool sqlServer = true)
        {
            var ctxConfig = new DbContextConfiguration
            {
                ConfigType = sqlServer ? ConfigurationType.SQLServer : ConfigurationType.SQLite,
                ConnectionString = connectionString
            };
            bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(new EventStoreDbContext(ctxConfig), typeof(EventStoreDbContext)));
            //if ioc not used
            EventStoreManager.DbContextConfiguration = ctxConfig;
        }
        

        #endregion

    }
}