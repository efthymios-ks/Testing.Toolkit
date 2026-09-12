using System.Text.Json.Nodes;

namespace Testing.Toolkit.Snapshots.Internal;

internal static class GridConverter
{
    /// <summary>
    /// Turns a rectangular grid of cells into a canonical <see cref="JsonNode"/>. With headers, the
    /// first row is captured as an ordered <c>headers</c> array (so column order and casing diff
    /// naturally) and the remaining rows become objects keyed by header. Without headers, the grid
    /// is a bare array of arrays.
    /// </summary>
    public static JsonNode ToNode(IReadOnlyList<IReadOnlyList<string?>> grid, bool hasHeader)
    {
        if (!hasHeader)
        {
            return RowsAsArray(grid, startAt: 0);
        }

        var headerRow = grid.Count > 0 ? grid[0] : [];
        var headers = new JsonArray();

        foreach (var header in headerRow)
        {
            headers.Add(JsonValue.Create(header ?? string.Empty));
        }

        var rows = new JsonArray();

        for (var rowIndex = 1; rowIndex < grid.Count; rowIndex++)
        {
            var row = grid[rowIndex];
            var rowObject = new JsonObject();

            for (var columnIndex = 0; columnIndex < headerRow.Count; columnIndex++)
            {
                var key = headerRow[columnIndex] ?? string.Empty;
                var value = columnIndex < row.Count ? row[columnIndex] : null;
                rowObject[key] = JsonValue.Create(value);
            }

            rows.Add(rowObject);
        }

        return new JsonObject
        {
            ["Headers"] = headers,
            ["Rows"] = rows,
        };
    }

    private static JsonArray RowsAsArray(IReadOnlyList<IReadOnlyList<string?>> grid, int startAt)
    {
        var array = new JsonArray();

        for (var rowIndex = startAt; rowIndex < grid.Count; rowIndex++)
        {
            var row = grid[rowIndex];
            var rowArray = new JsonArray();

            foreach (var cell in row)
            {
                rowArray.Add(JsonValue.Create(cell));
            }

            array.Add(rowArray);
        }

        return array;
    }
}
