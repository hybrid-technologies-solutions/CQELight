using BenchmarkDotNet.Attributes;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore;
using CQELight.EventStore.EFCore;
using CQELight.EventStore.EFCore.Common;
using CQELight.EventStore.EFCore.Models;
using CQELight.EventStore.Snapshots;
using CQELight_Benchmarks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQELight_Benchmarks.Benchmarks
{
    public enum DatabaseType
    {
        SQLite,
        SQLServer
    }
    public class EFCore_EventStoreBenchmark
    {
        #region BenchmarkDotNet

        public Guid AggregateId = Guid.NewGuid();

        [Params(DatabaseType.SQLite, DatabaseType.SQLServer)]
        public DatabaseType DatabaseType;

        [GlobalSetup]
        public void GlobalSetup()
        {
            CreateDatabase(DatabaseType);
            CleanDatabases();

        }

        [IterationSetup(Targets = new[] { nameof(StoreRangeDomainEvent), nameof(StoreRangeDomainEvent_Snapshot) })]
        public void IterationSetup()
        {
            CleanDatabases();
        }

        [GlobalSetup(Targets = new[] { nameof(RehydrateAggregate) })]
        public void GlobalSetup_Storage()
        {
            CreateDatabase(DatabaseType);
            CleanDatabases();
            StoreNDomainEvents();
        }

        [GlobalSetup(Targets = new[] { nameof(RehydrateAggregate_WithSnapshot) })]
        public void GlobalSetup_Storage_Snapshot()
        {
            CreateDatabase(DatabaseType);
            CleanDatabases();
            StoreNDomainEvents(new BasicSnapshotBehaviorProvider(new Dictionary<Type, ISnapshotBehavior>()
            {
                {typeof(TestEvent), new NumericSnapshotBehavior( 10) }
            }));
        }

        internal static void CreateDatabase(DatabaseType databaseType)
        {
            var options =
                databaseType == DatabaseType.SQLite
                ? new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(GetConnectionString_SQLite()).Options
                : new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlServer(GetConnectionString_SQLServer()).Options;
            using (var ctx = new EventStoreDbContext(options))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
        }

        #endregion

        #region Private methods

        private void CleanDatabases()
        {
            using (var ctx = new EventStoreDbContext(GetDbOptions()))
            {
                ctx.RemoveRange(ctx.Set<Event>());
                ctx.RemoveRange(ctx.Set<Snapshot>());
                ctx.SaveChanges();
            }
        }

        private void StoreNDomainEvents(ISnapshotBehaviorProvider provider = null)
        {
            var store = new EFEventStore(GetConfig(provider));
            for (int i = 0; i < 1000; i++)
            {
                store.StoreDomainEventAsync(new TestEvent(Guid.NewGuid(), AggregateId) { AggregateStringValue = "test", AggregateIntValue = i }).GetAwaiter().GetResult();
            }
        }

        private static string GetConnectionString_SQLServer()
            => new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()["EFCore_EventStore_Benchmarks:ConnectionString_SQLServer"];

        private static string GetConnectionString_SQLite()
            => new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()["EFCore_EventStore_Benchmarks:ConnectionString_SQLite"];

        private DbContextOptions<EventStoreDbContext> GetDbOptions()
        {
            switch (DatabaseType)
            {
                case DatabaseType.SQLite:
                    return new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(GetConnectionString_SQLite()).Options;
                default:
                    return new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlServer(GetConnectionString_SQLServer()).Options;
            }
        }

        private EFEventStoreOptions GetConfig(
            ISnapshotBehaviorProvider snapshotBehaviorProvider = null,
            BufferInfo bufferInfo = null)
        {
            EFEventStoreOptions options = null;
            switch (DatabaseType)
            {
                case DatabaseType.SQLite:
                    options = new EFEventStoreOptions(o => o.UseSqlite(GetConnectionString_SQLite()),
                        snapshotBehaviorProvider, bufferInfo);
                    break;
                default:
                    options = new EFEventStoreOptions(o => o.UseSqlite(GetConnectionString_SQLServer()),
                        snapshotBehaviorProvider, bufferInfo);
                    break;
            }
            return options;
        }

        #endregion

        #region Public methods

        [Benchmark]
        public async Task StoreSingleDomainEvent()
        {
            await new EFEventStore(GetConfig()).StoreDomainEventAsync(
            new TestEvent(Guid.NewGuid(), AggregateId)
            {
                AggregateIntValue = 1,
                AggregateStringValue = "test"
            });
        }

        [Benchmark]
        [Arguments(10, true)]
        [Arguments(10, false)]
        [Arguments(100, true)]
        [Arguments(100, false)]
        [Arguments(1000, true)]
        [Arguments(1000, false)]
        public async Task StoreRangeDomainEvent(int numberEvents, bool useBuffer)
        {
            var store = new EFEventStore(GetConfig(
                bufferInfo: useBuffer ? BufferInfo.Default : BufferInfo.Disabled
                ));
            for (int i = 0; i < numberEvents; i++)
            {
                await store.StoreDomainEventAsync(
                    new TestEvent(Guid.NewGuid(), AggregateId)
                    {
                        AggregateIntValue = 1,
                        AggregateStringValue = "test"
                    });
            }
        }


        [Benchmark]
        [Arguments(10, true)]
        [Arguments(10, false)]
        [Arguments(100, true)]
        [Arguments(100, false)]
        [Arguments(1000, true)]
        [Arguments(1000, false)]
        public async Task StoreRangeDomainEvent_Snapshot(int numberEvents, bool useBuffer)
        {
            var store = new EFEventStore(
                GetConfig(
                    new BasicSnapshotBehaviorProvider(new Dictionary<Type, ISnapshotBehavior>()
                    {
                        { typeof(TestEvent), new NumericSnapshotBehavior(10) }
                    }),
                    bufferInfo: useBuffer ? BufferInfo.Default : BufferInfo.Disabled));
            for (int i = 0; i < numberEvents; i++)
            {
                await store.StoreDomainEventAsync(
                   new TestEvent(Guid.NewGuid(), AggregateId)
                   {
                       AggregateIntValue = 1,
                       AggregateStringValue = "test"
                   });
            }
        }

        [Benchmark]
        public async Task GetEventsByAggregateId()
        {
            var evt
                = await new EFEventStore(GetConfig()).GetAllEventsByAggregateId<TestAggregate, Guid>(AggregateId).ToListAsync();
        }

        [Benchmark]
        public async Task RehydrateAggregate()
        {
            var store = new EFEventStore(GetConfig());
            var agg = await store.GetRehydratedAggregateAsync<TestAggregate>(AggregateId);
        }

        [Benchmark]
        public async Task RehydrateAggregate_WithSnapshot()
        {
            var store = new EFEventStore(
                GetConfig(new BasicSnapshotBehaviorProvider(
                    new Dictionary<Type, ISnapshotBehavior>()
                    {
                        { typeof(TestEvent), new NumericSnapshotBehavior(10) }
                    })));
            var agg = await store.GetRehydratedAggregateAsync<TestAggregate>(AggregateId);

        }

        #endregion
    }
}
