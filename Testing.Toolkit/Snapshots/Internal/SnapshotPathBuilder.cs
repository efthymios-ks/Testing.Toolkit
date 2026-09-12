namespace Testing.Toolkit.Snapshots.Internal;

internal static class SnapshotPathBuilder
{
    /// <summary>
    /// Builds the golden file path. Layout:
    /// <c>{projectRoot}/{options.Root}/{sourceRelativeDir}/{sourceFileStem}/{memberName}.{extension}</c>
    /// </summary>
    public static string Build(
        SnapshotOptions options,
        string sourceFilePath,
        string memberName,
        string extension
    )
    {
        var projectRoot = options.ProjectRoot ?? ProjectRootLocator.Locate(sourceFilePath);

        var sourceRelativeDir = Path.GetRelativePath(
            projectRoot,
            Path.GetDirectoryName(sourceFilePath)!
        );

        if (sourceRelativeDir == ".")
        {
            sourceRelativeDir = string.Empty;
        }

        var sourceFileStem = Path.GetFileNameWithoutExtension(sourceFilePath);

        return Path.Combine(
            projectRoot,
            options.Root,
            sourceRelativeDir,
            sourceFileStem,
            $"{memberName}.{extension.TrimStart('.')}"
        );
    }

    public static string BuildReceivedPath(string goldenPath)
    {
        var directory = Path.GetDirectoryName(goldenPath)!;
        var stem = Path.GetFileNameWithoutExtension(goldenPath);
        var extension = Path.GetExtension(goldenPath).TrimStart('.');

        return Path.Combine(directory, $"{stem}.received.{extension}");
    }
}
