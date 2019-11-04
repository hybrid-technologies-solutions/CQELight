using CQELight.DAL.Attributes;
using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CQELight.DAL.EFCore
{
    /// <summary>
    /// Base classe for implementing Entity Framework DbContext.
    /// </summary>
    public class BaseDbContext : DbContext
    {
        #region Members

        private readonly ILoggerFactory _loggerFactory;
        private bool _useSchema;

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new BaseDbContext with the specified connection to the database.
        /// </summary>
        /// <param name="options">DbContext options.</param>
        public BaseDbContext(DbContextOptions options)
            : this(options, null)
        {
        }

        /// <summary>
        /// Create a new BaseDbContext with the specified connection to the database, and a logger factory fo all 
        /// EF logs.
        /// </summary>
        /// <param name="options">DbContext options.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        public BaseDbContext(DbContextOptions options, ILoggerFactory loggerFactory)
            : base(options)
        {
            _loggerFactory = loggerFactory;
        }

        #endregion

        #region Overriden methods

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entities = this.GetType().Assembly.GetTypes().AsParallel()
                 .Where(t => typeof(IPersistableEntity).IsAssignableFrom(t)
                 && !t.IsDefined(typeof(IgnoreAttribute))).ToList();

            foreach (var item in entities.AsParallel())
            {
                modelBuilder.AutoMap(item, _useSchema, _loggerFactory);
            }
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            _useSchema = optionsBuilder.Options.Extensions.Any(e => e.GetType().Name.Contains("SqlServer"));
            if (System.Diagnostics.Debugger.IsAttached)
            {
                optionsBuilder.EnableSensitiveDataLogging();
            }
            if (_loggerFactory != null)
            {
                optionsBuilder.UseLoggerFactory(_loggerFactory);
            }
        }

        /// <summary>
        /// Cleaning up.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion

    }
}
