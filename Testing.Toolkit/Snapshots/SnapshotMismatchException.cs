namespace Testing.Toolkit.Snapshots;

/// <summary>
/// Thrown when the actual value does not match the golden. Carries the full list of differences and
/// the path to the <c>.received</c> file that was written next to the golden for external diffing.
/// </summary>
public sealed class SnapshotMismatchException(
    string expectedPath,
    string receivedPath,
    IReadOnlyList<SnapshotDifference> differences
    ) : Exception(BuildMessage(expectedPath, receivedPath, differences))
{
    public string ExpectedPath { get; } = expectedPath;

    public string ReceivedPath { get; } = receivedPath;

    public IReadOnlyList<SnapshotDifference> Differences { get; } = differences;

    private static string BuildMessage(string expectedPath, string receivedPath, IReadOnlyList<SnapshotDifference> differences)
    {
        var lines = new List<string>
        {
            $"Snapshot mismatch ({differences.Count} difference{(differences.Count == 1 ? "" : "s")}):",
        };

        foreach (var difference in differences)
        {
            lines.Add($"  {difference}");
        }

        lines.Add($"Expected: {expectedPath}");
        lines.Add($"Received: {receivedPath}");

        return string.Join(Environment.NewLine, lines);
    }
}
