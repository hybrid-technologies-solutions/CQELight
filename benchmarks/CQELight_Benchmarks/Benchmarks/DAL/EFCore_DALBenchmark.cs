using BenchmarkDotNet.Attributes;
using CQELight.DAL.EFCore;
using CQELight_Benchmarks.Benchmarks.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQELight_Benchmarks.Benchmarks.DAL
{
    public class EFCore_DALBenchmark
    {

        #region BenchmarkDotNet

        [Params(DatabaseType.SQLite, DatabaseType.SQLServer)]
        public DatabaseType DatabaseType;

        [Params(10, 100, 1000)]
        public int NbIterations;

        private List<Guid> _allIds = new List<Guid>();

        [GlobalSetup]
        public void GlobalSetup()
        {
            CreateDatabase();
            Console.WriteLine("Global Setup");

        }

        [IterationSetup(Targets = new[]{
            nameof(InsertComplex),
            nameof(InsertSimple)
        })]
        public void IterationSetup()
        {
            CleanDatabases();
        }

        [GlobalSetup(Targets = new[] {
            nameof(UpdateSimple),
            nameof(GetById),
            nameof(GetWithFilter),
            nameof(UpdateComplexWithoutInserts),
            nameof(UpdateComplexWithInserts)
        })]
        public void SeedSetup()
        {
            Console.WriteLine("Global setup with insertions");
            CreateDatabase();
            using (var ctx = new EFCoreBenchmarkDbContext(GetOptions()))
            {
                var azureLocation = new AzureLocation
                {
                    Country = "FR",
                    DataCenter = "Marseille"
                };
                ctx.Add(azureLocation);
                for (int i = 0; i < NbIterations; i++)
                {
                    var website = new WebSite
                    {
                        Url = "http://blogs.msdn.com/dotnet/" + i,
                        AzureLocation = azureLocation,
                        HyperLinks = new List<Hyperlink>
                        {
                            new Hyperlink
                            {
                                Value = "http://blogs.msd.com/dotnet/article/" + i
                            }
                        },
                        Posts = new List<Post>
                        {
                            new Post
                            {
                                Content = string.Concat(Enumerable.Repeat("Content ", 50)),
                                QuickUrl = "http://bit.ly/" + i
                            }
                        }
                    };
                    ctx.Add(website);
                    _allIds.Add(website.Id);
                }
                ctx.SaveChanges();
            }
        }

        private void CleanDatabases()
        {
            using (var ctx = new EFCoreBenchmarkDbContext(GetOptions()))
            {
                ctx.RemoveRange(ctx.Set<Hyperlink>());
                ctx.RemoveRange(ctx.Set<WebSite>());
                ctx.RemoveRange(ctx.Set<Post>());
                ctx.RemoveRange(ctx.Set<Comment>());
                ctx.RemoveRange(ctx.Set<AzureLocation>());
                ctx.RemoveRange(ctx.Set<User>());
                ctx.SaveChanges();
            }
        }

        private void CreateDatabase()
        {
            using (var ctx = new EFCoreBenchmarkDbContext(GetOptions()))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
        }

        private DbContextOptions GetOptions()
        {
            switch (DatabaseType)
            {
                case DatabaseType.SQLite: return new DbContextOptionsBuilder().UseSqlite("Filename=EFCoreBenchmarks.db").Options;
                default: return new DbContextOptionsBuilder().UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CQELight_EFCoreBenchmarks").Options;
            }
        }

        #endregion

        #region Benchmarks

        [Benchmark]
        public async Task InsertSimple()
        {
            using (var repo = new EFRepository<WebSite>(new EFCoreBenchmarkDbContext(GetOptions())))
            {
                for (int i = 0; i < NbIterations; i++)
                {
                    var website = new WebSite
                    {
                        Url = "http://blogs.msdn.com/dotnet/" + i
                    };
                    repo.MarkForInsert(website);
                }
                await repo.SaveAsync().ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task InsertComplex()
        {
            using (var repo = new EFRepository<WebSite>(new EFCoreBenchmarkDbContext(GetOptions())))
            {
                var azureLocation = new AzureLocation
                {
                    Country = "FR",
                    DataCenter = "Marseille"
                };
                for (int i = 0; i < NbIterations; i++)
                {
                    var website = new WebSite
                    {
                        Url = "http://blogs.msdn.com/dotnet/" + i,
                        AzureLocation = azureLocation,
                        HyperLinks = new List<Hyperlink>
                        {
                            new Hyperlink
                            {
                                Value = "http://blogs.msd.com/dotnet/article/" + i
                            }
                        },
                        Posts = new List<Post>
                        {
                            new Post
                            {
                                Content = string.Concat(Enumerable.Repeat("Content ", 50)),
                                QuickUrl = "http://bit.ly/" + i
                            }
                        }
                    };
                    repo.MarkForInsert(website);
                }
                await repo.SaveAsync().ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task GetById()
        {
            var r = new Random();
            for (int i = 0; i < NbIterations; i++)
            {
                var id = _allIds[r.Next(0, _allIds.Count - 1)];
                using (var repo = new EFRepository<WebSite>(new EFCoreBenchmarkDbContext(GetOptions())))
                {
                    var ws = await repo.GetByIdAsync(id).ConfigureAwait(false);
                }
            }
        }

        [Benchmark]
        public async Task GetWithFilter()
        {
            var r = new Random();
            for (int i = 0; i < NbIterations; i++)
            {
                using (var repo = new EFRepository<WebSite>(new EFCoreBenchmarkDbContext(GetOptions())))
                {
                    var ws = await repo.GetAsync(b => b.Url.EndsWith(r.Next(0, NbIterations - 1).ToString())).FirstOrDefaultAsync().ConfigureAwait(false);
                }
            }
        }

        [Benchmark]
        public async Task UpdateSimple()
        {
            var r = new Random();
            for (int i = 0; i < NbIterations; i++)
            {
                var id = _allIds[r.Next(0, _allIds.Count - 1)];
                using (var repo = new EFRepository<WebSite>(new EFCoreBenchmarkDbContext(GetOptions())))
                {
                    var ws = await repo.GetByIdAsync(id);

                    ws.Url = "http://blogs.msdn.net/dotnet" + i + "/newValue/" + r.Next();

                    await repo.SaveAsync().ConfigureAwait(false);
                }
            }
        }

        [Benchmark]
        public async Task UpdateComplexWithoutInserts()
        {
            var r = new Random();
            for (int i = 0; i < NbIterations; i++)
            {
                var id = _allIds[r.Next(0, _allIds.Count - 1)];
                using (var repo = new EFRepository<WebSite>(new EFCoreBenchmarkDbContext(GetOptions())))
                {
                    var ws = await repo.GetAsync(w => w.Id == id, includes: w => w.Posts).FirstOrDefaultAsync();

                    ws.Url = "http://blogs.msdn.net/dotnet" + i + "/newValue/" + r.Next();

                    ws.Posts.First().Published = true;
                    ws.Posts.First().Version = 2;

                    await repo.SaveAsync().ConfigureAwait(false);
                }
            }
        }

        [Benchmark]
        public async Task UpdateComplexWithInserts()
        {
            var r = new Random();
            for (int i = 0; i < NbIterations; i++)
            {
                var id = _allIds[r.Next(0, _allIds.Count - 1)];
                using (var repo = new EFRepository<WebSite>(new EFCoreBenchmarkDbContext(GetOptions())))
                {
                    var ws = await repo.GetAsync(w => w.Id == id, includes: w => w.Posts).FirstOrDefaultAsync();

                    ws.Url = "http://blogs.msdn.net/dotnet" + i + "/newValue/" + r.Next();

                    ws.Posts.First().Published = true;
                    ws.Posts.First().Version = 2;

                    await repo.SaveAsync().ConfigureAwait(false);
                }
            }
        }

        #endregion

    }
}
