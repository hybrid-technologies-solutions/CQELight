using CQELight.DAL.Interfaces;
using CQELight.IoC;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                    .UseAutofacAsIoC(c => { })
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
                    .UseAutofacAsIoC(c => { })
                    .UseEFCoreAsMainRepository(opt => opt.UseSqlite($"Filename={DbName}"))
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
                    .UseAutofacAsIoC(c => { })
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

        #endregion

    }
}
