namespace Testing.Toolkit.Substitutes.Internal;

/// <summary>
/// <c>repo.Received().Get(1)</c> dispatches Get like any other call. This marks the substitute so
/// that the call is checked against what was recorded instead of being recorded itself.
/// </summary>
internal static class VerificationContext
{
    [ThreadStatic]
    private static Expectation? _pending;

    public static void Expect(SubstituteState state, int? times)
        => _pending = new Expectation(state, times);

    /// <summary>Null unless the next call on this substitute is a verification.</summary>
    public static Expectation? TakeFor(SubstituteState state)
    {
        if (_pending is null || !ReferenceEquals(_pending.State, state))
        {
            return null;
        }

        var pending = _pending;
        _pending = null;

        return pending;
    }

    internal sealed record Expectation(SubstituteState State, int? Times);
}
