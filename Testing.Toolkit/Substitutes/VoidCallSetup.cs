using Testing.Toolkit.Substitutes.Internal;

namespace Testing.Toolkit.Substitutes;

/// <summary>What a <c>When</c> hands back, so a void member can still be told what to do.</summary>
public sealed class VoidCallSetup
{
    private readonly PendingSetup _pending;

    internal VoidCallSetup(PendingSetup pending)
        => _pending = pending;

    public void Throws(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _pending.State.AddRule(_pending.Specification, _ => throw exception);
    }

    public void Throws(Func<object?[], Exception> exceptionFactory)
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);

        _pending.State.AddRule(_pending.Specification, arguments => throw exceptionFactory(arguments));
    }

    /// <summary>Runs an action when the call happens, with the arguments it was given.</summary>
    public void Do(Action<object?[]> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _pending.State.AddRule(_pending.Specification, arguments =>
        {
            action(arguments);

            return null;
        });
    }
}
