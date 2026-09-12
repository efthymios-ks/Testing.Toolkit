using System.Text.Json.Nodes;

namespace Testing.Toolkit.Snapshots.Internal;

internal static class StructuralDiffer
{
    public static IReadOnlyList<SnapshotDifference> Diff(JsonNode? expected, JsonNode? actual)
    {
        var differences = new List<SnapshotDifference>();

        DiffNode(differences, path: string.Empty, expected, actual);

        return differences;
    }

    private static void DiffNode(
        List<SnapshotDifference> differences,
        string path,
        JsonNode? expected,
        JsonNode? actual
    )
    {
        var expectedKind = KindOf(expected);
        var actualKind = KindOf(actual);

        if (expectedKind != actualKind)
        {
            differences.Add(new(
                DisplayPath(path),
                expectedKind,
                actualKind,
                DifferenceKind.TypeMismatch
            ));

            return;
        }

        switch (expected)
        {
            case JsonObject expectedObject:
                DiffObject(differences, path, expectedObject, (JsonObject)actual!);
                break;
            case JsonArray expectedArray:
                DiffArray(differences, path, expectedArray, (JsonArray)actual!);
                break;
            case JsonValue expectedValue:
                DiffValue(differences, path, expectedValue, (JsonValue)actual!);
                break;
        }
    }

    private static void DiffObject(
        List<SnapshotDifference> differences,
        string path,
        JsonObject expected,
        JsonObject actual
    )
    {
        foreach (var (key, expectedChild) in expected)
        {
            var childPath = ChildPath(path, key);

            if (!actual.ContainsKey(key))
            {
                differences.Add(new(
                    childPath,
                    Render(expectedChild),
                    null,
                    DifferenceKind.MissingMember
                ));

                continue;
            }

            DiffNode(differences, childPath, expectedChild, actual[key]);
        }

        foreach (var (key, actualChild) in actual)
        {
            if (expected.ContainsKey(key))
            {
                continue;
            }

            differences.Add(new(
                ChildPath(path, key),
                null,
                Render(actualChild),
                DifferenceKind.ExtraMember
            ));
        }
    }

    private static void DiffArray(
        List<SnapshotDifference> differences,
        string path,
        JsonArray expected,
        JsonArray actual
    )
    {
        if (expected.Count != actual.Count)
        {
            differences.Add(new(
                DisplayPath(path),
                expected.Count.ToString(),
                actual.Count.ToString(),
                DifferenceKind.LengthMismatch
            ));

            return;
        }

        for (var index = 0; index < expected.Count; index++)
        {
            DiffNode(differences, IndexPath(path, index), expected[index], actual[index]);
        }
    }

    private static void DiffValue(
        List<SnapshotDifference> differences,
        string path,
        JsonValue expected,
        JsonValue actual
    )
    {
        var expectedText = expected.ToJsonString();
        var actualText = actual.ToJsonString();

        if (expectedText == actualText)
        {
            return;
        }

        differences.Add(new(
            DisplayPath(path),
            expectedText,
            actualText,
            DifferenceKind.ValueMismatch
        ));
    }

    private static string KindOf(JsonNode? node)
        => node switch
        {
            null => "null",
            JsonObject => "object",
            JsonArray => "array",
            _ => "value",
        };

    private static string ChildPath(string path, string key)
    {
        if (IsSimpleIdentifier(key))
        {
            return path.Length == 0 ? key : $"{path}.{key}";
        }

        return $"{path}[\"{Escape(key)}\"]";
    }

    private static string IndexPath(string path, int index)
        => $"{path}[{index}]";

    private static string DisplayPath(string path)
        => path.Length == 0 ? "$" : path;

    private static bool IsSimpleIdentifier(string key)
    {
        if (key.Length == 0)
        {
            return false;
        }

        foreach (var character in key)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Render(JsonNode? node)
        => node?.ToJsonString() ?? "null";
}
