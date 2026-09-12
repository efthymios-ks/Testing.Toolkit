using System.Text.Json.Nodes;

namespace Testing.Toolkit.Snapshots.Internal.Converters;

internal static class JsonConverter
{
    public static JsonNode? Convert(byte[] bytes)
        => JsonNode.Parse(bytes);
}
