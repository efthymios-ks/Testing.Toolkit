namespace Testing.Toolkit.Snapshots.Internal;

internal static class ProjectRootLocator
{
    public static string Locate(string sourceFilePath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFilePath)!);

        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.csproj").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate a .csproj walking up from '{sourceFilePath}'. " +
            $"Set {nameof(SnapshotOptions)}.{nameof(SnapshotOptions.ProjectRoot)} to an absolute path.");
    }
}
