using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Testing.Toolkit.Snapshots.Internal;
using Testing.Toolkit.Snapshots.Internal.Converters;

namespace Testing.Toolkit.Snapshots;

/// <summary>
/// Golden-file assertions. On first run the golden file is created from the actual bytes and the
/// test fails with <see cref="SnapshotMissingException"/>. On later runs the actual is compared
/// against the golden and, on mismatch, the actual is written next to the golden as
/// <c>{name}.received.{ext}</c> and <see cref="SnapshotMismatchException"/> is thrown with the full
/// list of differences.
/// </summary>
public static class Snapshot
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>Configuration shared by every assertion.</summary>
    public static SnapshotOptions Options { get; } = new();

    /// <summary>Byte-identical comparison. Use for opaque formats (png, pdf, protobuf, …).</summary>
    public static Task MatchAsync(
        byte[] actual,
        string extension = "bin",
        CancellationToken cancellationToken = default,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = ""
    ) => MatchCoreAsync(
        actual: actual,
        extension: extension,
        compare: BinaryCompare,
        memberName: memberName,
        sourceFilePath: sourceFilePath,
        cancellationToken: cancellationToken
    );

    /// <summary>Serializes <paramref name="actual"/> as indented JSON and diffs structurally.</summary>
    public static Task MatchJsonAsync(
        object? actual,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = ""
    ) => MatchCoreAsync(
        actual: JsonSerializer.SerializeToUtf8Bytes(actual, _serializerOptions),
        extension: "json",
        compare: JsonCompare,
        memberName: memberName,
        sourceFilePath: sourceFilePath,
        cancellationToken: cancellationToken
    );

    /// <summary>Diffs the bytes as JSON, structurally.</summary>
    public static Task MatchJsonAsync(
        byte[] actual,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = ""
    ) => MatchCoreAsync(
        actual: actual,
        extension: "json",
        compare: JsonCompare,
        memberName: memberName,
        sourceFilePath: sourceFilePath,
        cancellationToken: cancellationToken
    );

    /// <summary>
    /// Diffs the CSV text structurally. When <paramref name="hasHeader"/> is true, the first row is
    /// treated as headers — header names, order and case are all part of the comparison.
    /// </summary>
    public static Task MatchCsvAsync(
        string actual,
        bool hasHeader,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = ""
    ) => MatchCoreAsync(
        actual: Encoding.UTF8.GetBytes(actual),
        extension: "csv",
        compare: (expected, actualBytes) => StructuralDiffer.Diff(
            CsvConverter.Convert(expected, hasHeader),
            CsvConverter.Convert(actualBytes, hasHeader)
        ),
        memberName: memberName,
        sourceFilePath: sourceFilePath,
        cancellationToken: cancellationToken
    );

    /// <summary>
    /// Diffs the CSV bytes structurally. When <paramref name="hasHeader"/> is true, the first row is
    /// treated as headers — header names, order and case are all part of the comparison.
    /// </summary>
    public static Task MatchCsvAsync(
        byte[] actual,
        bool hasHeader,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = ""
    ) => MatchCoreAsync(
        actual: actual,
        extension: "csv",
        compare: (expected, actualBytes) => StructuralDiffer.Diff(
            CsvConverter.Convert(expected, hasHeader),
            CsvConverter.Convert(actualBytes, hasHeader)
        ),
        memberName: memberName,
        sourceFilePath: sourceFilePath,
        cancellationToken: cancellationToken
    );

    /// <summary>
    /// Diffs the workbook structurally, sheet by sheet. Comparison is data-only — formatting,
    /// formulas and styling are ignored. When <paramref name="hasHeader"/> is true, the first row of
    /// each sheet is treated as headers and diffed exactly (order and case included).
    /// </summary>
    public static Task MatchExcelAsync(
        byte[] actual,
        bool hasHeader,
        string extension = "xlsx",
        CancellationToken cancellationToken = default,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = ""
    ) => MatchCoreAsync(
        actual: actual,
        extension: extension,
        compare: (expected, actualBytes) => StructuralDiffer.Diff(
            ExcelConverter.Convert(expected, hasHeader),
            ExcelConverter.Convert(actualBytes, hasHeader)
        ),
        memberName: memberName,
        sourceFilePath: sourceFilePath,
        cancellationToken: cancellationToken
    );

    private static async Task MatchCoreAsync(
        byte[] actual,
        string extension,
        Func<byte[], byte[], IReadOnlyList<SnapshotDifference>> compare,
        string memberName,
        string sourceFilePath,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(sourceFilePath))
        {
            throw new InvalidOperationException(
                "Snapshot could not determine the caller's source file. Pass sourceFilePath explicitly."
            );
        }

        if (string.IsNullOrEmpty(memberName))
        {
            throw new InvalidOperationException(
                "Snapshot could not determine the caller's member name. Pass memberName explicitly."
            );
        }

        var goldenPath = SnapshotPathBuilder.Build(Options, sourceFilePath, memberName, extension);

        if (!File.Exists(goldenPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(goldenPath)!);
            await File.WriteAllBytesAsync(goldenPath, actual, cancellationToken);
            throw new SnapshotMissingException(goldenPath);
        }

        var expected = await File.ReadAllBytesAsync(goldenPath, cancellationToken);
        var differences = compare(expected, actual);
        var receivedPath = SnapshotPathBuilder.BuildReceivedPath(goldenPath);

        if (differences.Count == 0)
        {
            if (File.Exists(receivedPath))
            {
                File.Delete(receivedPath);
            }

            return;
        }

        await File.WriteAllBytesAsync(receivedPath, actual, cancellationToken);
        throw new SnapshotMismatchException(goldenPath, receivedPath, differences);
    }

    private static IReadOnlyList<SnapshotDifference> BinaryCompare(byte[] expected, byte[] actual)
    {
        if (expected.AsSpan().SequenceEqual(actual))
        {
            return [];
        }

        return
        [
            new (
                Path: "$",
                Expected: $"{expected.Length} bytes",
                Actual: $"{actual.Length} bytes",
                Kind: DifferenceKind.BinaryMismatch
            )
        ];
    }

    private static IReadOnlyList<SnapshotDifference> JsonCompare(byte[] expected, byte[] actual)
        => StructuralDiffer.Diff(
            JsonConverter.Convert(expected),
            JsonConverter.Convert(actual)
        );
}
