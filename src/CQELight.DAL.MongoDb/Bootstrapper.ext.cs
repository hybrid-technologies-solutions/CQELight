using CQELight.DAL.Interfaces;
using CQELight.DAL.MongoDb;
using CQELight.DAL.MongoDb.Adapters;
using CQELight.DAL.MongoDb.Serializers;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight
{
    public static class BootstrapperExtensions
    {

        #region Public static methods

        public static Bootstrapper UseMongoDbAsMainRepository(this Bootstrapper bootstrapper, MongoDbOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var service = new MongoDbDALBootstrapperService
            {
                BootstrappAction = (ctx) =>
                {
                    if (BsonSerializer.SerializerRegistry.GetSerializer<Type>() == null)
                    {
                        BsonSerializer.RegisterSerializer(typeof(Type), new TypeSerializer());
                    }
                    if (BsonSerializer.SerializerRegistry.GetSerializer<Guid>() == null)
                    {
                        BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer());
                    }

                    MongoDbContext.MongoClient = new MongoDB.Driver.MongoClient(options.Url);

                    var pack = new ConventionPack();
                    pack.Add(new IgnoreExtraElementsConvention(true));
                    ConventionRegistry.Register("CQELight conventions", pack, _ => true);

                    if (ctx.IsServiceRegistered(BootstrapperServiceType.IoC))
                    {
                        bootstrapper.AddIoCRegistration(new TypeRegistration<MongoDataReaderAdapter>(true));
                        bootstrapper.AddIoCRegistration(new TypeRegistration<MongoDataWriterAdapter>(true));

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
            };
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        #endregion

    }
}
