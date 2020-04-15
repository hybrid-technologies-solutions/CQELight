using CQELight.DAL.Attributes;
using CQELight.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System.Linq;
using System.Reflection;

namespace CQELight.DAL.EFCore
{
    /// <summary>
    /// Base classe for implementing Entity Framework DbContext.
    /// </summary>
    public class BaseDbContext : DbContext
    {
        #region Members

        private readonly ILoggerFactory loggerFactory;
        private bool _useSchema;
        private readonly EFCoreOptions? efOptions;

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new BaseDbContext with the specified connection to the database.
        /// </summary>
        /// <param name="options">DbContext options.</param>
        public BaseDbContext(DbContextOptions options)
            : base(options)
        {
            loggerFactory = new LoggerFactory(new[] { new DebugLoggerProvider() });
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
            this.loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Create a new BaseDbContext with the specified connection to the database, the logger factory for all
        /// EF Logs and CQELight EFCoreOptions
        /// </summary>
        /// <param name="options">DbContext options.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="efOptions">CQELight EFCore options</param>
        public BaseDbContext(DbContextOptions options, ILoggerFactory loggerFactory, EFCoreOptions efOptions)
            : base(options)
        {
            this.efOptions = efOptions;
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region Overriden methods

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Assembly assembly = GetType().Assembly;
            if (!string.IsNullOrWhiteSpace(efOptions?.ModelAssembly))
            {
                assembly = Assembly.Load(new AssemblyName(efOptions!.ModelAssembly));
            }
            var entities = assembly.GetTypes().AsParallel()
                 .Where(t => typeof(IPersistableEntity).IsAssignableFrom(t)
                 && !t.IsDefined(typeof(IgnoreAttribute))).ToList();

            foreach (var item in entities)
            {
                modelBuilder.AutoMap(item, _useSchema, loggerFactory);
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
            if (loggerFactory != null)
            {
                optionsBuilder.UseLoggerFactory(loggerFactory);
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
