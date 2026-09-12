namespace Testing.Toolkit.Snapshots;

/// <summary>
/// Thrown on the first run of a snapshot assertion. The golden file has been created from the actual
/// bytes; review the contents and re-run for the test to pass.
/// </summary>
public sealed class SnapshotMissingException(string path)
    : Exception($"Snapshot created at {path}. Review the contents and re-run the test.")
{
    public string Path { get; } = path;
}
