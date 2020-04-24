using CQELight.DAL.MongoDb.Adapters;
using CQELight.DAL.MongoDb.Mapping;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.DAL.MongoDb.Integration.Tests
{
    public class RepositoryBaseTests
    {
        #region Ctor & members

        private const string DatabaseName = "ThreadSafetyDatabase";

        public RepositoryBaseTests()
        {
            var c = new ConfigurationBuilder().AddJsonFile("test-config.json").Build();
            new Bootstrapper().UseMongoDbAsMainRepository(new MongoDbOptions(
                new MongoUrlBuilder
                {
                    Username = c["user"],
                    Password = c["password"],
                    Server = new MongoServerAddress(c["host"], int.Parse(c["port"]))
                }.ToMongoUrl(), DatabaseName)).Bootstrapp();
        }

        private IMongoCollection<T> GetCollection<T>()
            => MongoDbContext.MongoClient.GetDatabase(DatabaseName).GetCollection<T>(MongoDbMapper.GetMapping<T>().CollectionName);

        private void DeleteAll()
        {
            GetCollection<AzureLocation>().DeleteMany(FilterDefinition<AzureLocation>.Empty);
            GetCollection<Tag>().DeleteMany(FilterDefinition<Tag>.Empty);
            GetCollection<SpecificMongoObject>().DeleteMany(FilterDefinition<SpecificMongoObject>.Empty);
        }

        #endregion

        #region Thread safety

        [Fact]
        public async Task RepositoryBase_Must_Be_ThreadSafe()
        {
            try
            {
                DeleteAll();
                var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter());

                var tasks = new List<Task>();

                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            repo.MarkForInsert(new AzureLocation
                            {
                                Country = "country" + Guid.NewGuid(),
                                DataCenter = "center" + Guid.NewGuid()
                            });
                        }
                        await repo.SaveAsync();
                    }));
                }

                await Task.WhenAll(tasks);

                (await repo.GetAsync<AzureLocation>().CountAsync()).Should().Be(100);
            }
            finally
            {
                DeleteAll();
            }

        }

        #endregion

        #region MaxParallel

        [Fact]
        public async Task RepositoryBase_Must_Handle_MaxParallel_Calls()
        {
            try
            {
                var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter());

                var tasks = new List<Task>();

                for (int i = 0; i < 1000; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        repo.MarkForInsert(new AzureLocation
                        {
                            Country = "country" + Guid.NewGuid(),
                            DataCenter = "center" + Guid.NewGuid()
                        });
                    }));
                }

                await Task.WhenAll(tasks);

                await repo.SaveAsync(); // Throws MongoWaitQueueFullException if code doesn't handle the case

                (await repo.GetAsync<AzureLocation>().CountAsync()).Should().Be(1000);
            }
            finally
            {
                DeleteAll();
            }

        }

        #endregion

        #region Mongo Transactions

        [Fact]
        public async Task Records_Should_Be_Inserted_After_SaveAsync()
        {
            try
            {
                DeleteAll();
                var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter());

                var tasks = new List<Task>();

                for (int i = 0; i < 10; i++)
                {
                    repo.MarkForInsert(new SpecificMongoObject
                    {
                        IntValue = i,
                        Value = "val" + i
                    });
                    repo.MarkForInsert(new AzureLocation
                    {
                        Country = "country" + Guid.NewGuid(),
                        DataCenter = "center" + Guid.NewGuid()
                    });
                    repo.MarkForInsert(new Tag
                    {
                        Value = "Tag " + i.ToString()
                    });
                }

                (await repo.GetAsync<AzureLocation>().CountAsync()).Should().Be(0);
                (await repo.GetAsync<Tag>().CountAsync()).Should().Be(0);
                (await repo.GetAsync<SpecificMongoObject>().CountAsync()).Should().Be(0);

                await repo.SaveAsync();

                (await repo.GetAsync<SpecificMongoObject>().CountAsync()).Should().Be(10);
                (await repo.GetAsync<AzureLocation>().CountAsync()).Should().Be(10);
                (await repo.GetAsync<Tag>().CountAsync()).Should().Be(10);
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Recors_Should_Be_Updated_After_SaveAsync()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
