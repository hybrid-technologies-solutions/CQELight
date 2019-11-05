﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MongoDB.Driver;
using System.Linq;
using CQELight.Tools.Extensions;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using CQELight.DAL.MongoDb.Mapping;

namespace CQELight.DAL.MongoDb.Integration.Tests
{
    public class MongoRepositoryTests : BaseUnitTestClass
    {
        #region Ctor & members

        public MongoRepositoryTests()
        {
            var c = new ConfigurationBuilder().AddJsonFile("test-config.json").Build();
            new Bootstrapper().UseMongoDbAsMainRepository(new MongoDbOptions(c["user"], c["password"], $"{c["host"]}:{c["port"]}")).Bootstrapp();
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

        #region Get

        [Fact]
        public async Task MongoRepository_SimpleGet_NoFilter_NoOrder_NoIncludes_Should_Returns_All()
        {
            try
            {
                var collection = GetCollection<WebSite>();
                await collection.InsertManyAsync(new[]
                {
                    new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    },
                    new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    }
                });

                using (var repo = new MongoRepository<WebSite>())
                {
                    var sites = await repo.GetAsync().ToList().ConfigureAwait(false);
                    sites.Should().HaveCount(2);
                    sites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    sites.Any(s => s.Url.Contains("microsoft")).Should().BeTrue();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_SimpleGet_WithFilter_NoOrder_NoIncludes_Should_Returns_FilteredItemsOnly()
        {
            try
            {
                var collection = GetCollection<WebSite>();
                await collection.InsertManyAsync(new[]
                {
                    new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    },
                    new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    }
                });

                using (var repo = new MongoRepository<WebSite>())
                {
                    var sites = await repo.GetAsync(w => w.Url.Contains("msdn")).ToList().ConfigureAwait(false);
                    sites.Should().HaveCount(1);
                    sites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    sites.Any(s => s.Url.Contains("microsoft")).Should().BeFalse();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_SimpleGet_NoFilter_NohOrder_NoIncludes_WithDeleted_Should_GetAll()
        {
            try
            {
                var collection = GetCollection<WebSite>();
                await collection.InsertManyAsync(new[]
                {
                    new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    },
                    new WebSite
                    {
                        Url = "https://www.microsoft.com",
                        Deleted = true,
                        DeletionDate = DateTime.Now
                    }
                });

                using (var repo = new MongoRepository<WebSite>())
                {
                    var sites = await repo.GetAsync(includeDeleted: true).ToList().ConfigureAwait(false);
                    sites.Should().HaveCount(2);
                    sites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    sites.Any(s => s.Url.Contains("microsoft")).Should().BeTrue();
                    sites.Any(s => s.Deleted).Should().BeTrue();
                    sites.Any(s => !s.Deleted).Should().BeTrue();

                    var undeletedSites = await repo.GetAsync().ToList().ConfigureAwait(false);
                    undeletedSites.Should().HaveCount(1);
                    undeletedSites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    undeletedSites.Any(s => s.Url.Contains("microsoft")).Should().BeFalse();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_SimpleGet_NoFilter_WithOrder_NoIncludes_Should_Respect_Order()
        {
            try
            {
                var collection = GetCollection<WebSite>();
                await collection.InsertManyAsync(new[]
                {
                    new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    },
                    new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    }
                });

                using (var repo = new MongoRepository<WebSite>())
                {
                    var sites = await repo.GetAsync(orderBy: b => b.Url).ToList().ConfigureAwait(false);
                    sites.Should().HaveCount(2);
                    sites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    sites.Any(s => s.Url.Contains("microsoft")).Should().BeTrue();
                    sites[0].Url.Should().Contain("msdn");
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_SimpleGet_NoFilter_NoOrder_WithIncludes_Should_GetLinkedData()
        {
            try
            {
                var collection = GetCollection<WebSite>();
                await collection.InsertManyAsync(new[]
                {
                   new WebSite
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
                    },
                   new WebSite
                    {
                        Url = "https://www.microsoft.com",
                        HyperLinks = new List<Hyperlink>
                            {
                                new Hyperlink
                                {
                                    Value = "https://www.microsoft.com"
                                },
                                new Hyperlink
                                {
                                    Value = "https://www.visualstudio.com"
                                }
                            }
                    }
                });

                using (var repo = new MongoRepository<WebSite>())
                {
                    var sites = await repo.GetAsync(includes: w => w.HyperLinks).ToList().ConfigureAwait(false);
                    sites.Should().HaveCount(2);
                    sites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    sites.Any(s => s.Url.Contains("microsoft")).Should().BeTrue();

                    var site = sites.Find(s => s.Url.Contains("msdn"));
                    site.HyperLinks.Should().HaveCount(2);
                    site.HyperLinks.Any(u => u.Value.Contains("blogs.")).Should().BeTrue();
                    site.HyperLinks.Any(u => u.Value.Contains("blogs2.")).Should().BeTrue();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Get_Element_WithComplexIndexes()
        {
            try
            {
                var collection = GetCollection<Comment>();
                var user = new User { Name = "toto", LastName = "titi" };
                var post = new Post();
                await collection.InsertManyAsync(new[]
                {
                   new Comment("comment", user, post ),
                   new Comment("comment2", user, post)
                }); ;

                using (var repo = new MongoRepository<Comment>())
                {
                    var comments = await repo.GetAsync().ToList().ConfigureAwait(false);
                    comments.Should().HaveCount(2);
                    comments.Any(s => s.Value.Contains("comment")).Should().BeTrue();
                    comments.Any(s => s.Value.Contains("comment2")).Should().BeTrue();
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task MongoRepository_GetByIdAsync_Guid_AsExpected()
        {
            try
            {
                Guid id = Guid.NewGuid();
                var collection = GetCollection<WebSite>();
                var site1 = new WebSite
                {
                    Url = "https://blogs.msdn.net",
                };
                var site2 = new WebSite
                {
                    Url = "https://www.microsoft.com"
                };
                site1.FakePersistenceId(id);
                await collection.InsertManyAsync(new[] { site1, site2 });

                using (var repo = new MongoRepository<WebSite>())
                {
                    var result = await repo.GetByIdAsync(id).ConfigureAwait(false);
                    result.Should().NotBeNull();
                    result.Url.Should().Be("https://blogs.msdn.net");
                }
            }
            finally
            {
                DeleteAll();
            }
        }
        [Fact]
        public async Task MongoRepository_GetByIdAsync_CustomId_AsExpected()
        {
            try
            {
                using (var repo = new MongoRepository<Hyperlink>())
                {
                    repo.MarkForInsert(new Hyperlink
                    {
                        Value = "http://www.microsoft.com"
                    });
                    await repo.SaveAsync();
                }

                using (var repo = new MongoRepository<Hyperlink>())
                {
                    var result = await repo.GetByIdAsync("http://www.microsoft.com").ConfigureAwait(false);
                    result.Should().NotBeNull();
                    result.Value.Should().Be("http://www.microsoft.com");
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_GetByIdAsync_ComplexId_AsExpected()
        {
            try
            {
                using (var repo = new MongoRepository<AzureLocation>())
                {
                    repo.MarkForInsert(new AzureLocation
                    {
                        Country = "Allemagne",
                        DataCenter = "Munich"
                    });
                    repo.MarkForInsert(new AzureLocation
                    {
                        Country = "France",
                        DataCenter = "Paris"
                    });
                    repo.MarkForInsert(new AzureLocation
                    {
                        Country = "France",
                        DataCenter = "Marseille"
                    });
                    await repo.SaveAsync();
                }

                using (var repo = new MongoRepository<AzureLocation>())
                {
                    var result = await repo.GetByIdAsync(new { Country = "France", DataCenter = "Paris" }).ConfigureAwait(false);
                    result.Should().NotBeNull();
                    result.Country.Should().Be("France");
                    result.DataCenter.Should().Be("Paris");
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region Insert

        [Fact]
        public async Task MongoRepository_Insert_AsExpected()
        {
            try
            {
                using (var repo = new MongoRepository<WebSite>())
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
        public async Task MongoRepository_Insert_Id_AlreadySet_AsExpected()
        {
            try
            {
                using (var repo = new MongoRepository<WebSite>())
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
        public async Task MongoRepository_Update_AsExpected()
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
                using (var repo = new MongoRepository<WebSite>())
                {
                    var w = await repo.GetAsync().FirstOrDefault().ConfigureAwait(false);
                    w.Url = "https://www.microsoft.com/office365";
                    repo.MarkForUpdate(w);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new MongoRepository<WebSite>())
                {
                    var testB = await repo.GetAsync().ToList().ConfigureAwait(false);
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
        public async Task MongoRepository_Update_NotExisting_InBDD_AsExpected()
        {
            try
            {
                using (var repo = new MongoRepository<WebSite>())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    b.FakePersistenceId(Guid.NewGuid());
                    repo.MarkForUpdate(b);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new MongoRepository<WebSite>())
                {
                    var testB = await repo.GetAsync().ToList().ConfigureAwait(false);
                    testB.Should().HaveCount(1);
                    testB[0].Url.Should().Be("http://www.microsoft.com");
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region DeletionTest

        [Fact]
        public void MongoRepository_MarkIdForDelete_IdNotFound()
        {
            try
            {
                using (var repo = new MongoRepository<WebSite>())
                {
                    Assert.Throws<InvalidOperationException>(() => repo.MarkIdForDelete(Guid.NewGuid()));
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_Physical_Deletion_ById()
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

                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync().Count()).Should().Be(1);
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    var entity = await repo.GetAsync().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkIdForDelete(id, true);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(0);
                    (await repo.GetAsync().Count()).Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_Logical_Deletion_ById()
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

                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync().Count()).Should().Be(1);
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    var entity = await repo.GetAsync().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkIdForDelete(id);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(1);
                    var entity = await repo.GetAsync(includeDeleted: true).FirstOrDefault();
                    entity.DeletionDate.Should().BeSameDateAs(DateTime.Today);
                    (await repo.GetAsync().Count()).Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_Physical_Deletion()
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

                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync().Count()).Should().Be(1);
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    var entity = await repo.GetAsync().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkForDelete(entity, true);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(0);
                    (await repo.GetAsync().Count()).Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task MongoRepository_Logical_Deletion()
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

                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync().Count()).Should().Be(1);
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(1);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    var entity = await repo.GetAsync().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkForDelete(entity);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new MongoRepository<WebSite>())
                {
                    (await repo.GetAsync(includeDeleted: true).Count()).Should().Be(1);
                    var entity = await repo.GetAsync(includeDeleted: true).FirstOrDefault();
                    entity.DeletionDate.Should().BeSameDateAs(DateTime.Today);
                    (await repo.GetAsync().Count()).Should().Be(0);
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
