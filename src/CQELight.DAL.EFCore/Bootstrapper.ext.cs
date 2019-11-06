using CQELight.DAL.Common;
using CQELight.DAL.EFCore;
using CQELight.DAL.EFCore.Adapters;
using CQELight.DAL.Interfaces;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CQELight
{
    public static class BootstrapperExt
    {
        #region Private static members

        private static readonly Dictionary<string, Type> s_ContextTypesPerAssembly
            = new Dictionary<string, Type>();

        #endregion

        #region Public static methods

        /// <summary>
        /// Configure EF Core as repository implementation. 
        /// This methods uses a single DbContext for all repositories
        /// </summary>
        /// <param name="bootstrapper">Bootstrapper instance</param>
        /// <param name="dbContext">Instance of BaseDbContext to use</param>
        /// <param name="options">Custom options to use of using EF.</param>
        [Obsolete("Using a single DbContext for all adapters is not recommended. This method shouldn't be used anymore.")]
        public static Bootstrapper UseEFCoreAsMainRepository(this Bootstrapper bootstrapper, BaseDbContext dbContext,
            EFCoreOptions options = null)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }
            InitializeBootstrapperService(
                bootstrapper,
                (ctx) =>
                     {
                         if (ctx.IsServiceRegistered(BootstrapperServiceType.IoC))
                         {
                             var entities = ReflectionTools.GetAllTypes()
                                .Where(t => typeof(IPersistableEntity).IsAssignableFrom(t)
                                   && !t.IsAbstract
                                   && t.IsClass).ToList();
                             foreach (var item in entities)
                             {
                                 var efRepoType = typeof(EFRepository<>).MakeGenericType(item);
                                 var dataReaderRepoType = typeof(IDataReaderRepository<>).MakeGenericType(item);
                                 var databaseRepoType = typeof(IDatabaseRepository<>).MakeGenericType(item);
                                 var dataUpdateRepoType = typeof(IDataUpdateRepository<>).MakeGenericType(item);

                                 bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(dbContext, dbContext.GetType()));

                                 bootstrapper
                                     .AddIoCRegistration(new FactoryRegistration(() => efRepoType.CreateInstance(dbContext),
                                         efRepoType, dataUpdateRepoType, databaseRepoType, dataReaderRepoType));
                             }
                         }
                     },
                options);
            return bootstrapper;
        }

        /// <summary>
        /// Configure EF Core as repository implementation. 
        /// This methods uses a single database configuration and create dynamically all context
        /// from every concerned assembly.
        /// </summary>
        /// <param name="bootstrapper">Bootstrapper instance</param>
        /// <param name="optionsBuilderCfg">Options builder configuration lambda.</param>
        /// <param name="options">Custom options to use of using EF.</param>
        public static Bootstrapper UseEFCoreAsMainRepository(this Bootstrapper bootstrapper, Action<DbContextOptionsBuilder> optionsBuilderCfg,
            EFCoreOptions options = null)
        {
            if (optionsBuilderCfg == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilderCfg));
            }

            InitializeBootstrapperService(
                bootstrapper,
                (ctx) =>
            {
                if (ctx.IsServiceRegistered(BootstrapperServiceType.IoC))
                {
                    var dbContextOptionsBuilder = new DbContextOptionsBuilder();
                    optionsBuilderCfg(dbContextOptionsBuilder);

                    bootstrapper.AddIoCRegistration(new TypeRegistration<EFCoreDataReaderAdapter>(true));
                    bootstrapper.AddIoCRegistration(new TypeRegistration<EFCoreDataWriterAdapter>(true));
                    bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(dbContextOptionsBuilder.Options, typeof(DbContextOptions), typeof(DbContextOptions<BaseDbContext>)));

                    if (options != null)
                    {
                        bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(options, typeof(EFCoreOptions)));
                    }

                    var customDbContexts = ReflectionTools.GetAllTypes().Where(t => t.IsInHierarchySubClassOf(typeof(BaseDbContext)) && !t.IsAbstract && t.IsClass && t != typeof(BaseDbContext));
                    if (customDbContexts.Any())
                    {
                        customDbContexts.DoForEach(c => bootstrapper.AddIoCRegistration(new TypeRegistration(c, true)));
                    }
                    else
                    {
                        bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(BaseDbContext), typeof(BaseDbContext)));
                    }
                   

                    foreach (var item in ReflectionTools.GetAllTypes().Where(t => typeof(IPersistableEntity).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass).ToList())
                    {
                        var efRepoType = typeof(EFRepository<>).MakeGenericType(item);
                        var dataReaderRepoType = typeof(IDataReaderRepository<>).MakeGenericType(item);
                        var databaseRepoType = typeof(IDatabaseRepository<>).MakeGenericType(item);
                        var dataUpdateRepoType = typeof(IDataUpdateRepository<>).MakeGenericType(item);

                        Type ctxType = null;

                        if (s_ContextTypesPerAssembly.ContainsKey(item.Assembly.FullName))
                        {
                            ctxType = s_ContextTypesPerAssembly[item.Assembly.FullName];
                        }
                        else
                        {
                            ctxType = ReflectionTools
                                .GetAllTypes()
                                .Where(t => t.Assembly.FullName == item.Assembly.FullName)
                                .FirstOrDefault(c => c.IsSubclassOf(typeof(BaseDbContext)));
                            s_ContextTypesPerAssembly[item.Assembly.FullName] = ctxType;

                            bootstrapper
                                .AddIoCRegistration(new FactoryRegistration(() => ctxType.CreateInstance(dbContextOptionsBuilder.Options), ctxType));
                        }

                        if (ctxType == null)
                        {
                            throw new InvalidOperationException("Bootstrapper.UseEFCoreAsMainRepository() : " +
                                $"No DbContext found for assembly {item.Assembly.FullName}, but this assembly contains a " +
                                "some persistence entities. You need to create a specific class that inherits from BaseDbContext in this assembly to use this configuration method.");
                        }

                        bootstrapper
                            .AddIoCRegistration(new FactoryRegistration(() =>
                            {
                                var dbCtx = ctxType.CreateInstance(dbContextOptionsBuilder.Options);
                                return efRepoType.CreateInstance(dbCtx);
                            },
                            efRepoType, dataUpdateRepoType, databaseRepoType, dataReaderRepoType));
                    }
                }
            }, options);
            return bootstrapper;
        }

        #endregion

        #region Private methods

        private static void InitializeBootstrapperService(Bootstrapper bootstrapper, Action<BootstrappingContext> action, EFCoreOptions options = null)
        {
            var service = new DALEFCoreBootstrappService
            {
                BootstrappAction = action
            };
            bootstrapper.AddService(service);
            if (options != null)
            {
                EFCoreInternalExecutionContext.ParseEFCoreOptions(options);
            }
        }

        #endregion

    }
}