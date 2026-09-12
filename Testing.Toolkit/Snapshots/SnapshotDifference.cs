namespace Testing.Toolkit.Snapshots;

/// <summary>One point at which the actual value did not match the golden.</summary>
/// <param name="Path">Location of the mismatch, e.g. <c>orders[3].total</c> or <c>sheet["Sales"].row[1].col[2]</c>.</param>
/// <param name="Expected">The value the golden holds at that path, or a description of what was expected.</param>
/// <param name="Actual">The value the actual holds at that path, or a description of what was found.</param>
/// <param name="Kind">The kind of mismatch, so a caller can filter or format differently.</param>
public sealed record SnapshotDifference(string Path, string? Expected, string? Actual, DifferenceKind Kind)
{
    public override string ToString()
        => Kind switch
        {
            DifferenceKind.LengthMismatch => $"{Path}: length expected {Expected}, actual {Actual}",
            DifferenceKind.MissingMember => $"{Path}: missing (expected {Expected})",
            DifferenceKind.ExtraMember => $"{Path}: unexpected (actual {Actual})",
            DifferenceKind.TypeMismatch => $"{Path}: type expected {Expected}, actual {Actual}",
            DifferenceKind.BinaryMismatch => $"{Path}: bytes differ (expected {Expected}, actual {Actual})",
            _ => $"{Path}: expected {Expected}, actual {Actual}",
        };
}
