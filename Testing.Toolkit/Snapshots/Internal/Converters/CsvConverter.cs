using System.Text;
using System.Text.Json.Nodes;

namespace Testing.Toolkit.Snapshots.Internal.Converters;

internal static class CsvConverter
{
    public static JsonNode Convert(byte[] bytes, bool hasHeader)
    {
        var text = Encoding.UTF8.GetString(bytes);
        var grid = CsvParser.Parse(text);

        return GridConverter.ToNode([.. grid.Cast<IReadOnlyList<string?>>()], hasHeader);
    }
}
