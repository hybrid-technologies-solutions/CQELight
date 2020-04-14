using CQELight.DAL.Interfaces;
using CQELight.DAL.MongoDb;
using CQELight.DAL.MongoDb.Adapters;
using CQELight.DAL.MongoDb.Serializers;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;

namespace CQELight
{
    public static class BootstrapperExtensions
    {
        #region Private static members

        private static bool s_MongoStaticInit;
        private static readonly SemaphoreSlim s_ThreadSafety = new SemaphoreSlim(1);

        #endregion

        #region Public static methods

        public static Bootstrapper UseMongoDbAsMainRepository(this Bootstrapper bootstrapper, MongoDbOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var service = new MongoDbDALBootstrapperService
            (ctx =>
                {
                    InitiMongoDbStaticStuff();
                    MongoDbContext.DatabaseName = options.DatabaseName;
                    MongoDbContext.MongoClient = new MongoDB.Driver.MongoClient(options.Url);

                    if (ctx.IsServiceRegistered(BootstrapperServiceType.IoC))
                    {
                        bootstrapper.AddIoCRegistration(new TypeRegistration<MongoDataReaderAdapter>(true));
                        bootstrapper.AddIoCRegistration(new TypeRegistration<MongoDataWriterAdapter>(true));

                        bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(MongoDbContext.MongoClient, RegistrationLifetime.Singleton, typeof(MongoClient)));

                        var entities = ReflectionTools.GetAllTypes()
                        .Where(t => typeof(IPersistableEntity).IsAssignableFrom(t)).ToList();
                        foreach (var item in entities)
                        {
                            var mongoRepoType = typeof(MongoRepository<>).MakeGenericType(item);
                            var dataReaderRepoType = typeof(IDataReaderRepository<>).MakeGenericType(item);
                            var databaseRepoType = typeof(IDatabaseRepository<>).MakeGenericType(item);
                            var dataUpdateRepoType = typeof(IDataUpdateRepository<>).MakeGenericType(item);

                            bootstrapper
                                .AddIoCRegistration(new FactoryRegistration(() => mongoRepoType.CreateInstance(),
                                    mongoRepoType, dataUpdateRepoType, databaseRepoType, dataReaderRepoType));
                        }
                    }
                }
            );
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        #endregion

        #region Private static methods

        private static void InitiMongoDbStaticStuff()
        {
            if (!s_MongoStaticInit)
            {
                s_ThreadSafety.Wait();
                try
                {
                    if (!s_MongoStaticInit)
                    {
                        BsonSerializer.RegisterSerializer(typeof(Type), new TypeSerializer());
                        BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer());
                        var pack = new ConventionPack
                        {
                            new IgnoreExtraElementsConvention(true)
                        };
                        ConventionRegistry.Register("CQELight conventions", pack, _ => true);

                        s_MongoStaticInit = true;
                    }
                }
                finally
                {
                    s_ThreadSafety.Release();
                }
            }
        }

        #endregion

    }
}
