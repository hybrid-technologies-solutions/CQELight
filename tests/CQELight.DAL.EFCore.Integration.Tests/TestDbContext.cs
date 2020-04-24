﻿using Microsoft.EntityFrameworkCore;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace CQELight.DAL.EFCore.Integration.Tests
{

    public class TestDbContext : BaseDbContext
    {
        public TestDbContext()
            : base(
                  //new DbContextOptionsBuilder().UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TDD_Base;Trusted_Connection=True;MultipleActiveResultSets=true;").Options)
                  new DbContextOptionsBuilder().UseSqlite("Filename=TDD.db").Options
                  )
        {
        }

        public TestDbContext(DbContextOptions options)
            : base(options)
        { }
    }
}
