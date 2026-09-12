using Testing.Toolkit.Snapshots;

namespace Testing.Toolkit.Tests.Snapshots;

[Collection("Snapshot")]
public sealed class SnapshotTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _sourceFile;

    public SnapshotTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"snapshot-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        _sourceFile = Path.Combine(_tempRoot, "Fake.cs");

        Snapshot.Options.ProjectRoot = _tempRoot;
        Snapshot.Options.Root = "Snapshots";
    }

    public void Dispose()
    {
        Snapshot.Options.ProjectRoot = null;
        Snapshot.Options.Root = "Snapshots";

        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Match_WhenGoldenIsMissing_ShouldCreateFileAndThrow()
    {
        // Arrange
        var actual = new byte[] { 1, 2, 3 };
        var member = nameof(Match_WhenGoldenIsMissing_ShouldCreateFileAndThrow);

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchAsync(actual, extension: "bin", memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        Assert.True(File.Exists(thrown.Path));
        Assert.Equal(actual, await File.ReadAllBytesAsync(thrown.Path));
        Assert.EndsWith(Path.Combine("Snapshots", "Fake", $"{member}.bin"), thrown.Path);
    }

    [Fact]
    public async Task Match_WhenBytesAreIdentical_ShouldPass()
    {
        // Arrange
        var actual = new byte[] { 10, 20, 30 };
        var member = nameof(Match_WhenBytesAreIdentical_ShouldPass);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchAsync(actual, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act & Assert (no throw)
        await Snapshot.MatchAsync(actual, memberName: member, sourceFilePath: _sourceFile);
    }

    [Fact]
    public async Task Match_WhenBytesDiffer_ShouldWriteReceivedAndThrow()
    {
        // Arrange
        var member = nameof(Match_WhenBytesDiffer_ShouldWriteReceivedAndThrow);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchAsync([1, 2, 3], memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchAsync([9, 8, 7], memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        Assert.True(File.Exists(thrown.ReceivedPath));
        Assert.Equal(new byte[] { 9, 8, 7 }, await File.ReadAllBytesAsync(thrown.ReceivedPath));
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal(DifferenceKind.BinaryMismatch, difference.Kind);
    }

    [Fact]
    public async Task Match_WhenMismatchIsResolved_ShouldRemoveReceivedFile()
    {
        // Arrange
        var member = nameof(Match_WhenMismatchIsResolved_ShouldRemoveReceivedFile);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchAsync([1], memberName: member, sourceFilePath: _sourceFile)
        );

        var mismatch = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchAsync([2], memberName: member, sourceFilePath: _sourceFile)
        );

        Assert.True(File.Exists(mismatch.ReceivedPath));

        // Act
        await Snapshot.MatchAsync([1], memberName: member, sourceFilePath: _sourceFile);

        // Assert
        Assert.False(File.Exists(mismatch.ReceivedPath));
    }

    [Fact]
    public async Task MatchJson_WhenValueDiffers_ShouldReportPathAndValues()
    {
        // Arrange
        var member = nameof(MatchJson_WhenValueDiffers_ShouldReportPathAndValues);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchJsonAsync(new
            {
                name = "Alice",
                age = 30
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchJsonAsync(new
            {
                name = "Alice",
                age = 31
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("age", difference.Path);
        Assert.Equal("30", difference.Expected);
        Assert.Equal("31", difference.Actual);
        Assert.Equal(DifferenceKind.ValueMismatch, difference.Kind);
    }

    [Fact]
    public async Task MatchJson_WhenArrayLengthsDiffer_ShouldReportLengthOnlyAndNotDescend()
    {
        // Arrange
        var member = nameof(MatchJson_WhenArrayLengthsDiffer_ShouldReportLengthOnlyAndNotDescend);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchJsonAsync(new
            {
                items = new[] { 1, 2, 3 }
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(() =>
            Snapshot.MatchJsonAsync(new
            {
                items = new[] { 9 }
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("items", difference.Path);
        Assert.Equal("3", difference.Expected);
        Assert.Equal("1", difference.Actual);
        Assert.Equal(DifferenceKind.LengthMismatch, difference.Kind);
    }

    [Fact]
    public async Task MatchJson_WhenArraysMatchInLength_ShouldReportPerIndexDifferences()
    {
        // Arrange
        var member = nameof(MatchJson_WhenArraysMatchInLength_ShouldReportPerIndexDifferences);
        await Assert.ThrowsAsync<SnapshotMissingException>(() =>
            Snapshot.MatchJsonAsync(new
            {
                items = new[] { 1, 2, 3 }
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(() =>
            Snapshot.MatchJsonAsync(new
            {
                items = new[] { 1, 9, 3 }
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("items[1]", difference.Path);
    }

    [Fact]
    public async Task MatchJson_WhenMemberIsMissing_ShouldReportMissing()
    {
        // Arrange
        var member = nameof(MatchJson_WhenMemberIsMissing_ShouldReportMissing);
        await Assert.ThrowsAsync<SnapshotMissingException>(() =>
            Snapshot.MatchJsonAsync(new
            {
                a = 1,
                b = 2
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(() =>
            Snapshot.MatchJsonAsync(new
            {
                a = 1
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("b", difference.Path);
        Assert.Equal(DifferenceKind.MissingMember, difference.Kind);
    }

    [Fact]
    public async Task MatchJson_WhenMemberIsExtra_ShouldReportExtra()
    {
        // Arrange
        var member = nameof(MatchJson_WhenMemberIsExtra_ShouldReportExtra);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchJsonAsync(new
            {
                a = 1
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchJsonAsync(new
            {
                a = 1,
                b = 2
            }, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("b", difference.Path);
        Assert.Equal(DifferenceKind.ExtraMember, difference.Kind);
    }

    [Fact]
    public async Task MatchCsv_WhenHeaderCaseDiffers_ShouldReport()
    {
        // Arrange
        var member = nameof(MatchCsv_WhenHeaderCaseDiffers_ShouldReport);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchCsvAsync("Name,Age\nAlice,30", hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchCsvAsync("name,Age\nAlice,30", hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        Assert.Contains(thrown.Differences, difference
            => difference.Path == "Headers[0]"
            && difference.Kind == DifferenceKind.ValueMismatch
        );
    }

    [Fact]
    public async Task MatchCsv_WhenHeaderOrderDiffers_ShouldReport()
    {
        // Arrange
        var member = nameof(MatchCsv_WhenHeaderOrderDiffers_ShouldReport);
        await Assert.ThrowsAsync<SnapshotMissingException>(() =>
            Snapshot.MatchCsvAsync("Name,Age\nAlice,30", hasHeader: true, memberName: member, sourceFilePath: _sourceFile));

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchCsvAsync("Age,Name\n30,Alice", hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        Assert.Contains(thrown.Differences, difference => difference.Path == "Headers[0]");
        Assert.Contains(thrown.Differences, difference => difference.Path == "Headers[1]");
    }

    [Fact]
    public async Task MatchCsv_WhenRowCountsDiffer_ShouldReportLength()
    {
        // Arrange
        var member = nameof(MatchCsv_WhenRowCountsDiffer_ShouldReportLength);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchCsvAsync("A,B\n1,2\n3,4", hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchCsvAsync("A,B\n1,2", hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("Rows", difference.Path);
        Assert.Equal(DifferenceKind.LengthMismatch, difference.Kind);
    }

    [Fact]
    public async Task MatchCsv_WhenCellDiffers_ShouldReportRowAndHeaderPath()
    {
        // Arrange
        var member = nameof(MatchCsv_WhenCellDiffers_ShouldReportRowAndHeaderPath);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchCsvAsync("Name,Age\nAlice,30\nBob,40", hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchCsvAsync("Name,Age\nAlice,30\nBob,41", hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("Rows[1].Age", difference.Path);
    }

    [Fact]
    public async Task MatchCsv_WhenHasHeaderIsFalse_ShouldUsePositionalPaths()
    {
        // Arrange
        var member = nameof(MatchCsv_WhenHasHeaderIsFalse_ShouldUsePositionalPaths);
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchCsvAsync("a,b\nc,d", hasHeader: false, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act
        var thrown = await Assert.ThrowsAsync<SnapshotMismatchException>(()
            => Snapshot.MatchCsvAsync("a,b\nc,X", hasHeader: false, memberName: member, sourceFilePath: _sourceFile)
        );

        // Assert
        var difference = Assert.Single(thrown.Differences);
        Assert.Equal("[1][1]", difference.Path);
    }

    [Fact]
    public async Task MatchCsv_WhenQuotedFieldsContainCommasAndNewlines_ShouldRoundTrip()
    {
        // Arrange
        var member = nameof(MatchCsv_WhenQuotedFieldsContainCommasAndNewlines_ShouldRoundTrip);
        var csv = "Name,Note\n\"Alice, A.\",\"line 1\nline 2\"";
        await Assert.ThrowsAsync<SnapshotMissingException>(()
            => Snapshot.MatchCsvAsync(csv, hasHeader: true, memberName: member, sourceFilePath: _sourceFile)
        );

        // Act & Assert (no throw)
        await Snapshot.MatchCsvAsync(csv, hasHeader: true, memberName: member, sourceFilePath: _sourceFile);
    }
}
