using CQELight.DAL.MongoDb.Adapters;
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
    public class MongoDataReaderAdapterTests : BaseUnitTestClass
    {
        #region Ctor & members

        public MongoDataReaderAdapterTests()
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

        #region GetAsync

        [Fact]
        public async Task SimpleGet_NoFilter_NoOrder_Should_Returns_All()
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

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var sites = await repo.GetAsync<WebSite>().ToList().ConfigureAwait(false);
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
        public async Task SimpleGet_WithFilter_NoOrder_Should_Returns_FilteredItemsOnly()
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

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var sites = await repo.GetAsync<WebSite>(w => w.Url.Contains("msdn")).ToList().ConfigureAwait(false);
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
        public async Task SimpleGet_NoFilter_NohOrder_WithDeleted_Should_GetAll()
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

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var sites = await repo.GetAsync<WebSite>(includeDeleted: true).ToList().ConfigureAwait(false);
                    sites.Should().HaveCount(2);
                    sites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    sites.Any(s => s.Url.Contains("microsoft")).Should().BeTrue();
                    sites.Any(s => s.Deleted).Should().BeTrue();
                    sites.Any(s => !s.Deleted).Should().BeTrue();

                    var undeletedSites = await repo.GetAsync<WebSite>().ToList().ConfigureAwait(false);
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
        public async Task SimpleGet_NoFilter_WithOrder_Should_Respect_Order()
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

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var sites = await repo.GetAsync<WebSite>(orderBy: b => b.Url).ToList().ConfigureAwait(false);
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
        public async Task SimpleGet_NoFilter_NoOrder_Should_GetLinkedData()
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

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var sites = await repo.GetAsync<WebSite>().ToList().ConfigureAwait(false);
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

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var comments = await repo.GetAsync<Comment>().ToList().ConfigureAwait(false);
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
        public async Task GetByIdAsync_Guid_AsExpected()
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

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var result = await repo.GetByIdAsync<WebSite>(id).ConfigureAwait(false);
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
        public async Task GetByIdAsync_CustomId_AsExpected()
        {
            try
            {
                var collection = GetCollection<Hyperlink>();
                await collection.InsertOneAsync(new Hyperlink
                {
                    Value = "http://www.microsoft.com"
                });

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var result = await repo.GetByIdAsync<Hyperlink>("http://www.microsoft.com").ConfigureAwait(false);
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
        public async Task GetByIdAsync_ComplexId_AsExpected()
        {
            try
            {
                var collection = GetCollection<AzureLocation>();
                var locations = new[] {
                    new AzureLocation
                    {
                        Country = "Allemagne",
                        DataCenter = "Munich"
                    },
                    new AzureLocation
                    {
                        Country = "France",
                        DataCenter = "Paris"
                    },new AzureLocation
                    {
                        Country = "France",
                        DataCenter = "Marseille"
                    }
                };
                await collection.InsertManyAsync(locations);

                using (var repo = new RepositoryBase(new MongoDataReaderAdapter(), new MongoDataWriterAdapter()))
                {
                    var result = await repo.GetByIdAsync<AzureLocation>(new { Country = "France", DataCenter = "Paris" }).ConfigureAwait(false);
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

    }
}
