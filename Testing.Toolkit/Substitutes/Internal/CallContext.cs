namespace Testing.Toolkit.Substitutes.Internal;

/// <summary>
/// <c>repo.Get(1).Returns(order)</c> reads as one statement, but Returns runs after Get has already
/// been dispatched. The call parks here so Returns can find out what it is configuring.
/// </summary>
internal static class CallContext
{
    [field: ThreadStatic]
    public static PendingSetup? LastCall { get; set; }

    public static PendingSetup Take()
    {
        var lastCall = LastCall
            ?? throw new SubstituteException(
                "No substitute call to configure. Write it as substitute.Method(...).Returns(value)."
            );

        LastCall = null;

        return lastCall;
    }
}
