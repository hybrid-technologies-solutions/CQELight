﻿using CQELight.DAL.MongoDb.Adapters;
using CQELight.DAL.MongoDb.Mapping;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.DAL.MongoDb.Integration.Tests.Adapters
{
    public class MongoDataWriterAdapterTests : BaseUnitTestClass
    {
        #region Ctor & members

        public MongoDataWriterAdapterTests()
        {
            if (!Global.s_globalInit)
            {
                var c = new ConfigurationBuilder().AddJsonFile("test-config.json").Build();
                new Bootstrapper().UseMongoDbAsMainRepository(new MongoDbOptions(c["user"], c["password"], $"{c["host"]}:{c["port"]}")).Bootstrapp();

                Global.s_globalInit = true;
            }
            DeleteAll();
        }

        private IMongoCollection<T> GetCollection<T>()
            => MongoDbContext.MongoClient
                    .GetDatabase("DefaultDatabase")
                    .GetCollection<T>(MongoDbMapper.GetMapping<T>().CollectionName);

        private void DeleteAll() 
        {
            GetCollection<AzureLocation>().DeleteMany(FilterDefinition<AzureLocation>.Empty);
            GetCollection<Hyperlink>().DeleteMany(FilterDefinition<Hyperlink>.Empty);
            GetCollection<WebSite>().DeleteMany(FilterDefinition<WebSite>.Empty);
            GetCollection<Post>().DeleteMany(FilterDefinition<Post>.Empty);
            GetCollection<Comment>().DeleteMany(FilterDefinition<Comment>.Empty);
            GetCollection<User>().DeleteMany(FilterDefinition<User>.Empty);
        }

        #endregion

        #region Insert

        [Fact]
        public async Task Insert_AsExpected()
        {
            try
            {
                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    repo.MarkForInsert(b);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                var collection = GetCollection<WebSite>();
                var testB = collection.Find(FilterDefinition<WebSite>.Empty).ToList();
                testB.Should().HaveCount(1);
                testB[0].Url.Should().Be("http://www.microsoft.com");
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Insert_Id_AlreadySet_AsExpected()
        {
            try
            {
                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    typeof(WebSite).GetProperty("Id").SetValue(b, Guid.NewGuid());
                    repo.MarkForInsert(b);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                var collection = GetCollection<WebSite>();
                var testB = collection.Find(FilterDefinition<WebSite>.Empty).ToList();
                testB.Should().HaveCount(1);
                testB[0].Url.Should().Be("http://www.microsoft.com");
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_AsExpected()
        {
            try
            {
                var collection = GetCollection<WebSite>();
                await collection.InsertManyAsync(new[]
                {
                    new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    }
                });
                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var w = await repo.GetAsync<WebSite>().FirstOrDefault().ConfigureAwait(false);
                    w.Url = "https://www.microsoft.com/office365";
                    repo.MarkForUpdate(w);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var testB = await repo.GetAsync<WebSite>().ToList().ConfigureAwait(false);
                    testB.Should().HaveCount(1);
                    testB[0].Url.Should().Be("https://www.microsoft.com/office365");
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Update_NotExisting_InBDD_AsExpected()
        {
            try
            {
                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    b.FakePersistenceId(Guid.NewGuid());
                    repo.MarkForUpdate(b);
                    await Assert.ThrowsAsync<InvalidOperationException>(repo.SaveAsync);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region Delete

        [Fact]
        public void MarkIdForDelete_IdNotFound()
        {
            try
            {
                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    Assert.Throws<InvalidOperationException>(() => repo.MarkIdForDelete<WebSite>(Guid.NewGuid()));
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Physical_Deletion_ById()
        {
            try
            {
                Guid id = Guid.NewGuid();
                var collection = GetCollection<WebSite>();
                var website = new WebSite
                {
                    Url = "https://blogs.msdn.net",
                    HyperLinks = new List<Hyperlink>
                        {
                            new Hyperlink
                            {
                                Value = "https://blogs.msdn.net"
                            },
                            new Hyperlink
                            {
                                Value = "https://blogs2.msdn.net"
                            }
                        }
                };
                website.FakePersistenceId(id);
                await collection.InsertOneAsync(website);

                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(1);
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var entity = await repo.GetAsync<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkIdForDelete<WebSite>(id, true);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(0);
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Logical_Deletion_ById()
        {
            try
            {
                Guid id = Guid.NewGuid();
                var collection = GetCollection<WebSite>();
                var website = new WebSite
                {
                    Url = "https://blogs.msdn.net",
                    HyperLinks = new List<Hyperlink>
                        {
                            new Hyperlink
                            {
                                Value = "https://blogs.msdn.net"
                            },
                            new Hyperlink
                            {
                                Value = "https://blogs2.msdn.net"
                            }
                        }
                };
                website.FakePersistenceId(id);
                await collection.InsertOneAsync(website);

                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(1);
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var entity = await repo.GetAsync<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkIdForDelete<WebSite>(id);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(1);
                    var entity = await repo.GetAsync<WebSite>(includeDeleted: true).FirstOrDefault();
                    entity.DeletionDate.Should().BeSameDateAs(DateTime.Today);
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Physical_Deletion()
        {
            try
            {
                Guid id = Guid.NewGuid();
                var collection = GetCollection<WebSite>();
                var website = new WebSite
                {
                    Url = "https://blogs.msdn.net",
                    HyperLinks = new List<Hyperlink>
                        {
                            new Hyperlink
                            {
                                Value = "https://blogs.msdn.net"
                            },
                            new Hyperlink
                            {
                                Value = "https://blogs2.msdn.net"
                            }
                        }
                };
                website.FakePersistenceId(id);
                await collection.InsertOneAsync(website);

                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(1);
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var entity = await repo.GetAsync<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkForDelete(entity, true);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(0);
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Logical_Deletion()
        {
            try
            {
                Guid id = Guid.NewGuid();
                var collection = GetCollection<WebSite>();
                var website = new WebSite
                {
                    Url = "https://blogs.msdn.net",
                    HyperLinks = new List<Hyperlink>
                        {
                            new Hyperlink
                            {
                                Value = "https://blogs.msdn.net"
                            },
                            new Hyperlink
                            {
                                Value = "https://blogs2.msdn.net"
                            }
                        }
                };
                website.FakePersistenceId(id);
                await collection.InsertOneAsync(website);

                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(1);
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var entity = await repo.GetAsync<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkForDelete(entity);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo =new DatabaseRepository(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    (await repo.GetAsync<WebSite>(includeDeleted: true).Count()).Should().Be(1);
                    var entity = await repo.GetAsync<WebSite>(includeDeleted: true).FirstOrDefault();
                    entity.DeletionDate.Should().BeSameDateAs(DateTime.Today);
                    (await repo.GetAsync<WebSite>().Count()).Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion
    }
}
