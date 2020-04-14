using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace CQELight.DAL.MongoDb.Serializers
{
    internal class TypeSerializer : SerializerBase<Type>
    {
        #region Overriden methods

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Type value)
        {
            var typeAsString = value?.AssemblyQualifiedName;
            if (!string.IsNullOrWhiteSpace(typeAsString))
            {
                context.Writer.WriteString(typeAsString);
            }
            else
            {
                context.Writer.WriteString(typeof(object).AssemblyQualifiedName);
            }
        }

        public override Type Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var typeAsString = context.Reader.ReadString();
            if (!string.IsNullOrWhiteSpace(typeAsString))
            {
                return Type.GetType(typeAsString);
            }
            return typeof(object);
        }

        #endregion

    }
}
