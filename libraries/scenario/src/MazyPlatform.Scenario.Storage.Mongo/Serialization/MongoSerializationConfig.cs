namespace MazyPlatform.Scenario.Storage.Mongo.Serialization;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

internal static class MongoSerializationConfig
{
    private static readonly Lock _lockObject = new();
    private static bool _configured;

    public static void Configure()
    {
        lock (_lockObject)
        {
            if (_configured)
                return;

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            var conventionPack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
            };

            ConventionRegistry.Register(
                nameof(MazyPlatform) + ".Scenario.Storage.Mongo",
                conventionPack,
                static t => t.Namespace?.StartsWith(
                    "MazyPlatform.Scenario.Storage.Mongo",
                    StringComparison.Ordinal) == true);

            _configured = true;
        }
    }
}
