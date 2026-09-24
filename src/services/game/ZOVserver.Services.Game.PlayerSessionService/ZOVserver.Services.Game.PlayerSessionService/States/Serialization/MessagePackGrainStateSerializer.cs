using MessagePack;
using MongoDB.Bson;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;

namespace ZOVserver.Services.Game.PlayerSessionService.States.Serialization;

public class MessagePackGrainStateSerializer : IGrainStateSerializer
{
    public BsonValue Serialize<T>(T state)
    {
        return MessagePackSerializer.Serialize(state);
    }

    public T Deserialize<T>(BsonValue value)
    {
        return MessagePackSerializer.Deserialize<T>(value.AsByteArray);
    }
}