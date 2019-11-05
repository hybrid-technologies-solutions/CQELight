using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.DAL.EFCore.Integration.Tests
{
    #region Nested class

    internal class TestBlogEFRepository : EFRepository<WebSite>
    {
        public TestBlogEFRepository() : base(new TestDbContext())
        {
        }

        public WebSite GetWithPostAndComment()
            => DataSet
                .Include(b => b.Posts)
                .ThenInclude(p => p.Writer)
                .ThenInclude(u => u.Comments)
                .First();
    }

    #endregion

    public class EFRepositoryTests : BaseUnitTestClass
    {
        #region Ctor & members

        private static bool _isInit;

        public EFRepositoryTests()
        {
            if (!_isInit)
            {
                using (var ctx = new TestDbContext())
                {
                    ctx.Database.EnsureDeleted();
                    ctx.Database.Migrate();
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

        #endregion

        #region Get

        [Fact]
        public async Task EFRepository_SimpleGet_NoFilter_NoOrder_NoIncludes_Should_Returns_All()
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

                using (var repo = new TestBlogEFRepository())
                {
                    var sites = await repo.GetAsync().ToListAsync().ConfigureAwait(false);
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
        public async Task EFRepository_SimpleGet_WithFilter_NoOrder_NoIncludes_Should_Returns_FilteredItemsOnly()
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

                using (var repo = new TestBlogEFRepository())
                {
                    var sites = await repo.GetAsync(w => w.Url.Contains("msdn")).ToListAsync().ConfigureAwait(false);
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
        public async Task EFRepository_SimpleGet_NoFilter_NohOrder_NoIncludes_WithDeleted_Should_GetAll()
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

                using (var repo = new TestBlogEFRepository())
                {
                    var sites = await repo.GetAsync(includeDeleted: true).ToListAsync().ConfigureAwait(false);
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
        public async Task EFRepository_SimpleGet_NoFilter_WithOrder_NoIncludes_Should_Respect_Order()
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

                using (var repo = new TestBlogEFRepository())
                {
                    var sites = await repo.GetAsync(orderBy: b => b.Url).ToListAsync().ConfigureAwait(false);
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
        public async Task EFRepository_SimpleGet_NoFilter_NoOrder_WithIncludes_Should_GetLinkedData()
        {
            try
            {
                using (var ctx = new TestDbContext())
                {
                    ctx.Add(new WebSite
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
                    });
                    ctx.Add(new WebSite
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
                    });
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }

                using (var repo = new TestBlogEFRepository())
                {
                    var sites = await repo.GetAsync(includes: w => w.HyperLinks).ToListAsync().ConfigureAwait(false);
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

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task EFRepository_GetByIdAsync_AsExpected()
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

                using (var repo = new TestBlogEFRepository())
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

        #endregion

        #region Insert

        [Fact]
        public async Task EFRepository_Insert_AsExpected()
        {
            try
            {
                using (var repo = new TestBlogEFRepository())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    repo.MarkForInsert(b);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new TestBlogEFRepository())
                {
                    var testB = await repo.GetAsync().ToListAsync().ConfigureAwait(false);
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
        public async Task EFRepository_Insert_Id_AlreadySet_AsExpected()
        {
            try
            {
                using (var repo = new TestBlogEFRepository())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    typeof(WebSite).GetProperty("Id").SetValue(b, Guid.NewGuid());
                    repo.MarkForInsert(b);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new TestBlogEFRepository())
                {
                    var testB = await repo.GetAsync().ToListAsync().ConfigureAwait(false);
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
        public async Task EFRepository_Update_AsExpected()
        {
            try
            {
                using (var ctx = new TestDbContext())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    ctx.Add(b);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    var w = await repo.GetAsync().FirstOrDefaultAsync().ConfigureAwait(false);
                    w.Url = "https://www.microsoft.com";
                    repo.MarkForUpdate(w);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new TestBlogEFRepository())
                {
                    var testB = await repo.GetAsync().ToListAsync().ConfigureAwait(false);
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
        public async Task EFRepository_Update_NotExisting_InBDD_AsExpected()
        {
            try
            {
                using (var repo = new TestBlogEFRepository())
                {
                    var b = new WebSite
                    {
                        Url = "http://www.microsoft.com"
                    };
                    b.FakePersistenceId(Guid.NewGuid());
                    repo.MarkForUpdate(b);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new TestBlogEFRepository())
                {
                    var testB = await repo.GetAsync().ToListAsync().ConfigureAwait(false);
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
        public void EFRepository_MarkIdForDelete_IdNotFound()
        {
            try
            {
                using (var repo = new TestBlogEFRepository())
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
        public async Task EFRepository_Physical_Deletion_ById()
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
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get().Count().Should().Be(1);
                    repo.Get(includeDeleted: true).Count().Should().Be(1);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    var entity = repo.Get().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkIdForDelete(id, true);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get(includeDeleted: true).Count().Should().Be(0);
                    repo.Get().Count().Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task EFRepository_Logical_Deletion_ById()
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
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get().Count().Should().Be(1);
                    repo.Get(includeDeleted: true).Count().Should().Be(1);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    var entity = repo.Get().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkIdForDelete(id);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get(includeDeleted: true).Count().Should().Be(1);
                    var entity = repo.Get(includeDeleted: true).FirstOrDefault();
                    entity.DeletionDate.Should().BeSameDateAs(DateTime.Today);
                    repo.Get().Count().Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task EFRepository_Physical_Deletion()
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
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get().Count().Should().Be(1);
                    repo.Get(includeDeleted: true).Count().Should().Be(1);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    var entity = repo.Get().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkForDelete(entity, true);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get(includeDeleted: true).Count().Should().Be(0);
                    repo.Get().Count().Should().Be(0);
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
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get().Count().Should().Be(1);
                    repo.Get(includeDeleted: true).Count().Should().Be(1);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    var entity = repo.Get().FirstOrDefault();
                    entity.Should().NotBeNull();
                    repo.MarkForDelete(entity);
                    await repo.SaveAsync().ConfigureAwait(false);
                }
                using (var repo = new TestBlogEFRepository())
                {
                    repo.Get(includeDeleted: true).Count().Should().Be(1);
                    var entity = repo.Get(includeDeleted: true).FirstOrDefault();
                    entity.DeletionDate.Should().BeSameDateAs(DateTime.Today);
                    repo.Get().Count().Should().Be(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region NotNaviguable

        [Fact]
        public async Task EFRepository_Update_Blog_Should_Update_Posts_But_Not_User()
        {
            try
            {
                var site = new WebSite();
                site.Url = "http://msdn.dotnet.com";
                site.Posts = new List<Post>();
                site.Posts.Add(new Post
                {
                    Content = "test content",
                    WebSite = site,
                    QuickUrl = "http://msdn.dotnet.com/test",
                });
                var user = new User
                {
                    Name = "toto",
                    LastName = "titi"
                };
                user.Posts = new List<Post>();
                user.Posts.Add(site.Posts.First());
                user.Comments = new List<Comment>();
                user.Comments.Add(new Comment("test comment", user, site.Posts.First()));

                site.Posts.First().Writer = user;

                using (var repo = new TestBlogEFRepository())
                {
                    repo.MarkForInsert(site);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                WebSite dbSite = null;
                using (var repo = new TestBlogEFRepository())
                {
                    dbSite = repo.GetWithPostAndComment();
                }
                dbSite.Should().NotBeNull();

                var post = dbSite.Posts.First();

                post.QuickUrl += "2";
                post.Writer.LastName += "2";
                post.Writer.Name += "2";
                post.Writer.Comments.First().Value += "2";

                using (var repo = new TestBlogEFRepository())
                {
                    repo.MarkForUpdate(dbSite);
                    await repo.SaveAsync().ConfigureAwait(false);
                }

                using (var repo = new TestBlogEFRepository())
                {
                    dbSite = repo.GetWithPostAndComment();
                    dbSite.Should().NotBeNull();
                    post = dbSite.Posts.First();

                    post.QuickUrl.Should().Be("http://msdn.dotnet.com/test2");
                    post.Writer.Comments.First().Value.Should().Be("test comment2");
                    post.Writer.LastName.Should().Be("titi");
                    post.Writer.Name.Should().Be("toto");
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        #endregion

        #region DeleteCascade

        [Fact]
        public async Task EFRepository_MarkForDelete_DeleteBlog_Should_Delete_Posts_Logical_Cascade()
        {
            try
            {
                using (var ctx = new TestDbContext())
                {
                    var entity = new WebSite
                    {
                        Url = "http://dotnet.microsoft.com/"
                    };
                    var post = new Post
                    {
                        WebSite = entity,
                        Published = true,
                        PublicationDate = DateTime.Today,
                        QuickUrl = "http://dotnet.microsoft.com/abgfazJSQg2",
                        Version = 1,
                        Content = "test data"
                    };
                    entity.Posts = new List<Post> { post };
                    ctx.Add(entity);
                    ctx.SaveChanges();
                }

                using (var ctx = new TestDbContext())
                {
                    ctx.Set<WebSite>().Should().HaveCount(1);
                    ctx.Set<Post>().Should().HaveCount(1);
                }

                using (var rep = new TestBlogEFRepository())
                {
                    var b = await rep.GetAsync().FirstAsync().ConfigureAwait(false);
                    rep.MarkForDelete(b);
                    await rep.SaveAsync().ConfigureAwait(false);
                }

                using (var ctx = new TestDbContext())
                {
                    ctx.Set<WebSite>().Should().HaveCount(1);
                    ctx.Set<Post>().Should().HaveCount(1);

                    ctx.Set<WebSite>().ToList().Where(e => !e.Deleted).Should().HaveCount(0);
                    ctx.Set<Post>().ToList().Where(e => !e.Deleted).Should().HaveCount(1); // Le soft delete est à gérer niveau repository
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task EFRepository_MarkForDelete_DeleteBlog_Should_Delete_Posts_Physical_Cascade()
        {
            try
            {
                using (var ctx = new TestDbContext())
                {
                    var entity = new WebSite
                    {
                        Url = "http://dotnet.microsoft.com/"
                    };
                    var post = new Post
                    {
                        WebSite = entity,
                        Published = true,
                        PublicationDate = DateTime.Today,
                        QuickUrl = "http://dotnet.microsoft.com/abgfazJSQg2",
                        Version = 1,
                        Content = "test data"
                    };
                    entity.Posts = new List<Post> { post };
                    ctx.Add(entity);
                    ctx.SaveChanges();
                }

                using (var ctx = new TestDbContext())
                {
                    ctx.Set<WebSite>().Should().HaveCount(1);
                    ctx.Set<Post>().Should().HaveCount(1);
                }

                using (var rep = new TestBlogEFRepository())
                {
                    var b = await rep.GetAsync().FirstAsync().ConfigureAwait(false);
                    rep.MarkForDelete(b, true);
                    await rep.SaveAsync().ConfigureAwait(false);
                }

                using (var ctx = new TestDbContext())
                {
                    ctx.Set<WebSite>().Should().HaveCount(0);
                    ctx.Set<Post>().Should().HaveCount(0);
                }
            }
            finally
            {
                DeleteAll();
            }
        }

        [Fact]
        public async Task EFRepository_MarkForDelete_Logical_Deletion_Globally_Deactivated()
        {
            try
            {
                EFCoreInternalExecutionContext.DisableLogicalDeletion = true;
                using (var ctx = new TestDbContext())
                {
                    var entity = new WebSite
                    {
                        Url = "http://dotnet.microsoft.com/"
                    };
                    var post = new Post
                    {
                        WebSite = entity,
                        Published = true,
                        PublicationDate = DateTime.Today,
                        QuickUrl = "http://dotnet.microsoft.com/abgfazJSQg2",
                        Version = 1,
                        Content = "test data"
                    };
                    entity.Posts = new List<Post> { post };
                    ctx.Add(entity);
                    ctx.SaveChanges();
                }

                using (var ctx = new TestDbContext())
                {
                    ctx.Set<WebSite>().Should().HaveCount(1);
                    ctx.Set<Post>().Should().HaveCount(1);
                }

                using (var rep = new TestBlogEFRepository())
                {
                    var b = await rep.GetAsync().FirstAsync().ConfigureAwait(false);
                    rep.MarkForDelete(b);
                    await rep.SaveAsync().ConfigureAwait(false);
                }

                using (var ctx = new TestDbContext())
                {
                    ctx.Set<WebSite>().Should().HaveCount(0);
                    ctx.Set<Post>().Should().HaveCount(0);
                }
            }
            finally
            {
                EFCoreInternalExecutionContext.DisableLogicalDeletion = false;
                DeleteAll();
            }
        }

        #endregion

    }
}
