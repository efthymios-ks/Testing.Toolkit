namespace Testing.Toolkit.Snapshots;

/// <summary>
/// Configuration for <see cref="Snapshot"/>. Set once per test run, typically from a module initializer
/// or fixture.
/// </summary>
public sealed class SnapshotOptions
{
    /// <summary>
    /// Folder that holds every snapshot, relative to the test project root or an absolute path. The
    /// folder is created on demand.
    /// </summary>
    public string Root { get; set; } = "Snapshots";

    /// <summary>
    /// Absolute path to the test project root. When null, the root is discovered by walking up from
    /// the caller's source file until a <c>.csproj</c> is found.
    /// </summary>
    public string? ProjectRoot { get; set; }
}
