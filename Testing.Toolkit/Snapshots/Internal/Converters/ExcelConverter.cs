using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using ExcelDataReader;

namespace Testing.Toolkit.Snapshots.Internal.Converters;

internal static class ExcelConverter
{
    static ExcelConverter()
        // Required by ExcelDataReader to read legacy .xls (BIFF) files on .NET Core+.
        => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static JsonNode Convert(byte[] bytes, bool hasHeader)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet();

        var workbook = new JsonObject();

        foreach (DataTable sheet in dataSet.Tables)
        {
            var grid = new List<IReadOnlyList<string?>>();

            foreach (DataRow row in sheet.Rows)
            {
                var cells = new List<string?>(sheet.Columns.Count);

                foreach (var cell in row.ItemArray)
                {
                    cells.Add(cell is null or DBNull
                        ? null
                        : System.Convert.ToString(cell, CultureInfo.InvariantCulture));
                }

                grid.Add(cells);
            }

            workbook[sheet.TableName] = GridConverter.ToNode(grid, hasHeader);
        }

        return workbook;
    }
}
