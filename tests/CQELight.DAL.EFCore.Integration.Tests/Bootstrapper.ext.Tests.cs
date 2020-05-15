using CQELight.Bootstrapping.Notifications;
using CQELight.DAL.EFCore.Adapters;
using CQELight.DAL.Interfaces;
using CQELight.IoC;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.DAL.EFCore.Integration.Tests
{
    public class BootstrapperExtTests : BaseUnitTestClass
    {
        private const string DbName = "EFCoreDALTests.db";

        #region Ctor & members

        public BootstrapperExtTests()
        {
            using (var ctx = new TestDbContext(new DbContextOptionsBuilder().UseSqlite($"Filename={DbName}").Options))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
        }

        private void DeleteAll()
        {
            using (var ctx = new TestDbContext(new DbContextOptionsBuilder().UseSqlite($"Filename={DbName}").Options))
            {
                ctx.RemoveRange(ctx.Set<WebSite>());
                ctx.SaveChanges();
            }
        }

        #endregion

        #region UseEFCoreAsMainRepository

        [Fact]
        public void UseEFCoreAsMainRepository_With_DbContext_Instance_Should_Register_DbContext_In_IoC()
        {
            try
            {
                new Bootstrapper()
                    .UseAutofacAsIoC()
                    .UseEFCoreAsMainRepository(new TestDbContext())
                    .Bootstrapp();

                using (var scope = DIManager.BeginScope())
                {
                    scope.Resolve<TestDbContext>().Should().NotBeNull();
                    scope.Resolve<EFRepository<WebSite>>().Should().NotBeNull();
                    scope.Resolve<IDataReaderRepository<WebSite>>().Should().NotBeNull();
                    scope.Resolve<IDatabaseRepository<WebSite>>().Should().NotBeNull();
                    scope.Resolve<IDataUpdateRepository<WebSite>>().Should().NotBeNull();
                }
            }
            finally
            {
                DisableIoC();
            }
        }

        [Fact]
        public void UseEFCoreAsMainRepository_With_Options_Should_Register_DbContext_In_IoC()
        {
            try
            {
                new Bootstrapper()
                    .UseAutofacAsIoC()
                    .UseEFCoreAsMainRepository(opt => opt.UseSqlite($"Filename={DbName}"))
                    .Bootstrapp();

                using (var scope = DIManager.BeginScope())
                {
                    scope.Resolve<TestDbContext>().Should().NotBeNull();
                    scope.Resolve<EFRepository<WebSite>>().Should().NotBeNull();
                    scope.Resolve<IDataReaderRepository<WebSite>>().Should().NotBeNull();
                    scope.Resolve<IDatabaseRepository<WebSite>>().Should().NotBeNull();
                    scope.Resolve<IDataUpdateRepository<WebSite>>().Should().NotBeNull();

                    scope.Resolve<EFCoreDataReaderAdapter>().Should().NotBeNull();
                    scope.Resolve<EFCoreDataWriterAdapter>().Should().NotBeNull();
                    scope.Resolve<RepositoryBase>().Should().NotBeNull();
                }
            }
            finally
            {
                DisableIoC();
                if (File.Exists(DbName))
                {
                    File.Delete(DbName);
                }
            }
        }

        [Fact]
        public async Task UseEFCoreAsMainRepository_Options_Should_BeTaken_Into_Account()
        {
            try
            {
                new Bootstrapper()
                    .UseAutofacAsIoC()
                    .UseEFCoreAsMainRepository(opt => opt.UseSqlite($"Filename={DbName}"), new EFCoreOptions
                    {
                        DisableLogicalDeletion = true
                    })
                    .Bootstrapp();


                using (var scope = DIManager.BeginScope())
                {
                    var repo = scope.Resolve<EFRepository<WebSite>>();

                    repo.MarkForInsert(new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    });

                    await repo.SaveAsync();
                }

                using (var scope = DIManager.BeginScope())
                {
                    var repo = scope.Resolve<EFRepository<WebSite>>();
                    var ws = await repo.GetAsync().FirstOrDefaultAsync();

                    repo.MarkForDelete(ws, false); //Force it just to be sure
                    await repo.SaveAsync();
                }

                using (var scope = DIManager.BeginScope())
                {
                    var ctx = scope.Resolve<TestDbContext>();
                    ctx.Set<WebSite>().Count().Should().Be(0);
                }

                DeleteAll();

                using (var scope = DIManager.BeginScope())
                {
                    var repo = scope.Resolve<RepositoryBase>();

                    repo.MarkForInsert(new WebSite
                    {
                        Url = "https://blogs.msdn.net"
                    });

                    await repo.SaveAsync();
                }

                using (var scope = DIManager.BeginScope())
                {
                    var repo = scope.Resolve<RepositoryBase>();
                    var ws = await repo.GetAsync<WebSite>().FirstOrDefaultAsync();

                    repo.MarkForDelete(ws, false); //Force it just to be sure
                    await repo.SaveAsync();
                }

                using (var scope = DIManager.BeginScope())
                {
                    var ctx = scope.Resolve<TestDbContext>();
                    ctx.Set<WebSite>().Count().Should().Be(0);
                }
            }
            finally
            {
                DisableIoC();
                if (File.Exists(DbName))
                {
                    File.Delete(DbName);
                }
            }
        }

        [Fact]
        public void No_IoC_Should_Generate_Warning_Notification()
        {
            var notifs = new Bootstrapper()
                      .UseEFCoreAsMainRepository(opt => opt.UseSqlite($"Filename={DbName}"), new EFCoreOptions
                      {
                          DisableLogicalDeletion = true
                      })
                      .Bootstrapp();

            notifs.Should().HaveCount(1);
            notifs.First().Type.Should().Be(BootstrapperNotificationType.Warning);
        }

        #endregion

        #region DbContext

        [Fact]
        public void When_Bootstrapping_DbContext_Should_Be_Resolvable()
        {
            new Bootstrapper().UseEFCoreAsMainRepository(c => c.UseSqlite($"Filename={DbName}")).UseAutofacAsIoC().Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                scope.Resolve<DbContext>().Should().NotBeNull();
            }
        }

        #endregion

        #region Autofac

        [Fact]
        public async Task When_Using_AutoFac_As_IoC_Resolving_Should_Be_Working()
        {
            try
            {
                new Bootstrapper()
                    .UseEFCoreAsMainRepository(c => c.UseSqlite($"Filename={DbName}"))
                    .UseAutofacAsIoC()
                    .Bootstrapp();

                var scope = DIManager.BeginScope();
                var repo = scope.Resolve<RepositoryBase>();

                scope.Dispose();

                var b = new WebSite
                {
                    Url = "http://www.microsoft.com"
                };
                repo.MarkForInsert(b); // It would throw if repo has been disposed
                await repo.SaveAsync();

            }
            finally
            {
                using (var repo = DIManager.BeginScope().Resolve<RepositoryBase>())
                {
                    var websites = await repo.GetAsync<WebSite>().ToListAsync();
                    websites.ForEach(w => repo.MarkForDelete(w, true));
                    await repo.SaveAsync();
                }
            }
        }

        #endregion

        #region Microsoft DependencyInjection

        [Fact]
        public async Task When_Using_Microsoft_DI_As_IoC_Resolving_Should_Be_Working()
        {
            try
            {
                new Bootstrapper()
                    .UseEFCoreAsMainRepository(c => c.UseSqlite($"Filename={DbName}"))
                    .UseMicrosoftDependencyInjection(new ServiceCollection())
                    .Bootstrapp();

                var scope = DIManager.BeginScope();
                var repo = scope.Resolve<RepositoryBase>();

                var b = new WebSite
                {
                    Url = "http://www.microsoft.com"
                };
                repo.MarkForInsert(b); // Might throw on race condition if scope is disposed before this line
                await repo.SaveAsync();
            }
            finally
            {
                using (var repo = DIManager.BeginScope().Resolve<RepositoryBase>())
                {
                    var websites = await repo.GetAsync<WebSite>().ToListAsync();
                    websites.ForEach(w => repo.MarkForDelete(w, true));
                    await repo.SaveAsync();
                }
            }
        }

        #endregion

    }
}
