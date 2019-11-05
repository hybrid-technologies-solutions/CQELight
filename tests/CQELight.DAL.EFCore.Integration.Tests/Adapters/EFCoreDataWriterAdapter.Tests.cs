using CQELight.DAL.EFCore.Adapters;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.DAL.EFCore.Integration.Tests.Adapters
{
    public class EFCoreDataWriterAdapterTests : BaseUnitTestClass
    {
        #region Ctor & members

        private static bool _isInit;

        public EFCoreDataWriterAdapterTests()
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

        #region Insert

        [Fact]
        public async Task Insert_AsExpected()
        {
            try
            {
                using (var repo = GetRepository())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    repo.MarkForInsert(b);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var ctx = new TestDbContext())
                {
                    var testB = ctx.Set<WebSite>().ToList();
                    testB.Should().HaveCount(1);
                    testB[0].Url.Should().Be("http://www.microsoft.com");
                }
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
                using (var adapater = GetRepository())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    b.FakePersistenceId(Guid.NewGuid());
                    adapater.MarkForInsert(b);
                    await adapater.SaveAsync().ConfigureAwait(false);
                }

                using (var ctx = new TestDbContext())
                {
                    var testB = ctx.Set<WebSite>().ToList();
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

        #region Update

        [Fact]
        public async Task Update_AsExpected()
        {
            try
            {
                var b = new WebSite
                {
                    Url = "http://www.microsoft.com"
                };
                using (var ctx = new TestDbContext())
                {

                    ctx.Add(b);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }
                using (var adapater = GetRepository())
                {
                    b.Url = "https://www.microsoft.com";
                    adapater.MarkForUpdate(b);
                    await adapater.SaveAsync().ConfigureAwait(false);
                }

                using (var ctx = new TestDbContext())
                {
                    var testB = ctx.Set<WebSite>().ToList();
                    testB.Should().HaveCount(1);
                    testB[0].Url.Should().Be("https://www.microsoft.com");
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task Update_NotExisting_InBDD_Should_Throw_EFException()
        {
            try
            {
                using (var adapater = GetRepository())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    b.FakePersistenceId(Guid.NewGuid());
                    adapater.MarkForUpdate(b);
                    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(adapater.SaveAsync);
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
        public void MarkIdForDelete_ObjectNotFound_Should_Throws_InvalidOperationException()
        {
            try
            {
                using (var adapater = GetRepository())
                {
                    Assert.Throws<InvalidOperationException>(() => adapater.MarkIdForDelete<WebSite>(Guid.NewGuid()));
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
                Guid? id = null;
                using (var ctx = new TestDbContext())
                {
                    var entity = new WebSite
                    {
                        Url = "http://dotnet.microsoft.com/"
                    };
                    ctx.Add(entity);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                    id = entity.Id;
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(1);
                }
                using (var adapater = GetRepository())
                {
                    var entity = new TestDbContext().Set<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    adapater.MarkIdForDelete<WebSite>(id, true);
                    await adapater.SaveAsync().ConfigureAwait(false);
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(0);
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
                Guid? id = null;
                using (var ctx = new TestDbContext())
                {
                    var entity = new WebSite
                    {
                        Url = "http://dotnet.microsoft.com/"
                    };
                    ctx.Add(entity);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                    id = entity.Id;
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(1);
                    webSites.Count(w => w.Deleted).Should().Be(0);
                }
                using (var adapater = GetRepository())
                {
                    var entity = new TestDbContext().Set<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    adapater.MarkIdForDelete<WebSite>(id);
                    await adapater.SaveAsync().ConfigureAwait(false);
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(1);
                    webSites.Count(w => w.Deleted).Should().Be(1);
                    webSites[0].DeletionDate.Should().BeSameDateAs(DateTime.Today);
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
                using (var ctx = new TestDbContext())
                {
                    var entity = new WebSite
                    {
                        Url = "http://dotnet.microsoft.com/"
                    };
                    ctx.Add(entity);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(1);
                }
                using (var adapater = GetRepository())
                {
                    var entity = new TestDbContext().Set<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    adapater.MarkForDelete(entity, true);
                    await adapater.SaveAsync().ConfigureAwait(false);
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task EFRepository_Logical_Deletion()
        {
            try
            {
                using (var ctx = new TestDbContext())
                {
                    var entity = new WebSite
                    {
                        Url = "http://dotnet.microsoft.com/"
                    };
                    ctx.Add(entity);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(1);
                    webSites.Count(w => w.Deleted).Should().Be(0);
                }
                using (var adapater = GetRepository())
                {
                    var entity = new TestDbContext().Set<WebSite>().FirstOrDefault();
                    entity.Should().NotBeNull();
                    adapater.MarkForDelete<WebSite>(entity);
                    await adapater.SaveAsync().ConfigureAwait(false);
                }
                using (var ctx = new TestDbContext())
                {
                    var webSites = ctx.Set<WebSite>().ToList();
                    webSites.Count().Should().Be(1);
                    webSites.Count(w => w.Deleted).Should().Be(1);
                    webSites[0].DeletionDate.Should().BeSameDateAs(DateTime.Today);
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
