using CQELight.Tools.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace CQELight.EventStore.MongoDb.Common
{
    internal class SerializedObject
    {
        public string? Data { get; set; }
        public string? Type { get; set; }
    }

    internal class ObjectSerializer : SerializerBase<object>
    {
        #region Overriden methods

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
        {
            if (value != null)
            {
                context.Writer.WriteString(new SerializedObject { Data = value.ToJson(), Type = value.GetType().AssemblyQualifiedName }.ToJson());
            }
            else
            {
                context.Writer.WriteNull();
            }
        }

        public override object? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                var objAsJson = context.Reader.ReadString();
                if (!string.IsNullOrWhiteSpace(objAsJson))
                {
                    var serialized = objAsJson.FromJson<SerializedObject>();
                    if (!string.IsNullOrWhiteSpace(serialized?.Data))
                    {
                        return serialized.Data.FromJson(Type.GetType(serialized.Type));
                    }
                }
            }
            catch
            {
                //if any error on reading, then null should be returned
                context.Reader.ReadNull();
            }
            return null;
        }

        #endregion

    }
}
