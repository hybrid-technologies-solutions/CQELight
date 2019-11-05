using CQELight.DAL.EFCore.Adapters;
using CQELight.TestFramework;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.DAL.EFCore.Integration.Tests.Adapters
{
    public class EFCoreDataReaderAdapterTests : BaseUnitTestClass
    {
        #region Ctor & members

        private static bool _isInit;

        public EFCoreDataReaderAdapterTests()
        {
            if (!_isInit)
            {
                using (var ctx = new TestDbContext())
                {
                    ctx.Database.EnsureDeleted();
                    ctx.Database.EnsureCreated();
                }
                _isInit = true;
            }
            DeleteAll();
            EFCoreInternalExecutionContext.DisableLogicalDeletion = false;
        }

        private void DeleteAll()
        {
            using (var ctx = new TestDbContext())
            {
                ctx.RemoveRange(ctx.Set<AzureLocation>());
                ctx.RemoveRange(ctx.Set<Hyperlink>());
                ctx.RemoveRange(ctx.Set<WebSite>());
                ctx.RemoveRange(ctx.Set<Post>());
                ctx.RemoveRange(ctx.Set<Comment>());
                ctx.RemoveRange(ctx.Set<User>());
                ctx.RemoveRange(ctx.Set<Tag>());
                ctx.RemoveRange(ctx.Set<Word>());
                ctx.RemoveRange(ctx.Set<ComposedKeyEntity>());
                ctx.SaveChanges();
            }
        }

        private RepositoryBase GetRepository()
            => new RepositoryBase(
                    new EFCoreDataReaderAdapter(new TestDbContext()),
                    new EFCoreDataWriterAdapter(new TestDbContext())
                );

        #endregion

        #region GetAsync

        [Fact]
        public async Task SimpleGet_NoFilter_NoOrder_Should_Returns_All()
        {
            try
            {
                using (var ctx = new TestDbContext())
                {
                    ctx.Add(new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    });
                    ctx.Add(new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    });
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }

                using (var repo = GetRepository())
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
                using (var ctx = new TestDbContext())
                {
                    ctx.Add(new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    });
                    ctx.Add(new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    });
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }

                using (var adapter = GetRepository())
                {
                    var sites = await adapter.GetAsync<WebSite>(w => w.Url.Contains("msdn")).ToList().ConfigureAwait(false);
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
                using (var ctx = new TestDbContext())
                {
                    ctx.Add(new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    });
                    ctx.Add(new WebSite
                    {
                        Url = "https://www.microsoft.com",
                        Deleted = true,
                        DeletionDate = DateTime.Now
                    });
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }

                using (var adapter = GetRepository())
                {
                    var sites = await adapter.GetAsync<WebSite>(includeDeleted: true).ToList().ConfigureAwait(false);
                    sites.Should().HaveCount(2);
                    sites.Any(s => s.Url.Contains("msdn")).Should().BeTrue();
                    sites.Any(s => s.Url.Contains("microsoft")).Should().BeTrue();
                    sites.Any(s => s.Deleted).Should().BeTrue();
                    sites.Any(s => !s.Deleted).Should().BeTrue();
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
                using (var ctx = new TestDbContext())
                {
                    ctx.Add(new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    });
                    ctx.Add(new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    });
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }

                using (var adapter = GetRepository())
                {
                    var sites = await adapter.GetAsync<WebSite>(orderBy: b => b.Url).ToList().ConfigureAwait(false);
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

        #endregion

        #region GetByIdAsnc

        [Fact]
        public async Task GetByIdAsync_AsExpected()
        {
            try
            {
                Guid? id = null;
                using (var ctx = new TestDbContext())
                {
                    var w = new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    };
                    ctx.Add(w);
                    ctx.Add(new WebSite
                    {
                        Url = "https://www.microsoft.com"
                    });
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                    id = w.Id;
                }

                using (var adapter = GetRepository())
                {
                    WebSite result = await adapter.GetByIdAsync<WebSite>(id.Value).ConfigureAwait(false);
                    result.Should().NotBeNull();
                    result.Url.Should().Be("https://blogs.msdn.net");
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
