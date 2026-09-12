namespace Testing.Toolkit.Snapshots.Internal;

/// <summary>Minimal RFC 4180 CSV parser. Handles quoted fields, embedded quotes, and embedded newlines.</summary>
internal static class CsvParser
{
    public static List<List<string?>> Parse(string text)
    {
        var rows = new List<List<string?>>();
        var row = new List<string?>();
        var field = new System.Text.StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (insideQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"' when field.Length == 0:
                    insideQuotes = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    if (index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                    break;
                default:
                    field.Append(character);
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
