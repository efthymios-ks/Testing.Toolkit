namespace Testing.Toolkit.Substitutes.Internal;

/// <summary>
/// <c>repo.Get(1).Returns(order)</c> reads as one statement, but Returns runs after Get has already
/// been dispatched. The call parks here so Returns can find out what it is configuring.
/// </summary>
internal static class CallContext
{
    [ThreadStatic]
    private static PendingSetup? _lastCall;

    public static PendingSetup? LastCall
    {
        get => _lastCall;
        set => _lastCall = value;
    }

    public static PendingSetup Take()
    {
        var lastCall = _lastCall
            ?? throw new SubstituteException(
                "No substitute call to configure. Write it as substitute.Method(...).Returns(value)."
            );

        _lastCall = null;

        return lastCall;
    }
}
