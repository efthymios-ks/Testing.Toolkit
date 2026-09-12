using Testing.Toolkit.Substitutes.Internal;

namespace Testing.Toolkit.Substitutes;

public static class SubstituteExtensions
{
    /// <summary>Makes the call just written return this value.</summary>
    public static void Returns<TValue>(this TValue _, TValue value)
        => Configure(_ => value);

    /// <summary>Makes the call just written compute its result from the arguments it was given.</summary>
    public static void Returns<TValue>(this TValue _, Func<object?[], TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        Configure(arguments => valueFactory(arguments));
    }

    /// <summary>Returns each value in turn, then keeps returning the last one.</summary>
    public static void ReturnsInOrder<TValue>(this TValue _, params TValue[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0)
        {
            throw new SubstituteException("ReturnsInOrder needs at least one value.");
        }

        var callCount = 0;

        Configure(_ =>
        {
            var index = Math.Min(callCount, values.Length - 1);
            callCount++;

            return values[index];
        });
    }

    /// <summary>Makes the call just written throw.</summary>
    public static void Throws<TValue>(this TValue _, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Configure(_ => throw exception);
    }

    /// <summary>Makes the call just written throw an exception built per call.</summary>
    public static void Throws<TValue>(this TValue _, Func<object?[], Exception> exceptionFactory)
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);

        Configure(arguments => throw exceptionFactory(arguments));
    }

    /// <summary>
    /// Configures a member that returns nothing, which has no result to write Returns or Throws on.
    /// </summary>
    public static VoidCallSetup When<TInterface>(this TInterface substitute, Action<TInterface> call)
        where TInterface : class
    {
        ArgumentNullException.ThrowIfNull(call);

        // Rejects a non-substitute before the call runs, so the error names the real problem.
        StateOf(substitute);

        call(substitute);

        return new VoidCallSetup(CallContext.Take());
    }

    /// <summary>
    /// Asserts the call written next happened, the given number of times when one is stated.
    /// </summary>
    public static TInterface Received<TInterface>(this TInterface substitute, int? times = null)
        where TInterface : class
        => Verify(substitute, times);

    public static TInterface DidNotReceive<TInterface>(this TInterface substitute)
        where TInterface : class
        => Verify(substitute, times: 0);

    /// <summary>Every call made to the substitute, oldest first, as readable signatures.</summary>
    public static IReadOnlyList<string> ReceivedCalls<TInterface>(this TInterface substitute)
        where TInterface : class
        => [.. StateOf(substitute).Calls.Select(call => call.Specification.Describe())];

    public static void ClearReceivedCalls<TInterface>(this TInterface substitute)
        where TInterface : class
        => StateOf(substitute).ClearCalls();

    /// <summary>Forgets every Returns and Throws, leaving the recorded calls alone.</summary>
    public static void ClearSetups<TInterface>(this TInterface substitute)
        where TInterface : class
        => StateOf(substitute).ClearRules();

    private static void Configure(Func<object?[], object?> result)
    {
        var pending = CallContext.Take();

        pending.State.AddRule(pending.Specification, result);
    }

    /// <summary>
    /// Returns the substitute itself, so the call written next dispatches as normal and is then
    /// checked rather than recorded.
    /// </summary>
    private static TInterface Verify<TInterface>(TInterface substitute, int? times)
        where TInterface : class
    {
        var state = StateOf(substitute);

        VerificationContext.Expect(state, times);

        return substitute;
    }

    internal static SubstituteState StateOf<TInterface>(TInterface substitute)
        where TInterface : class
    {
        ArgumentNullException.ThrowIfNull(substitute);

        return substitute is SubstituteProxy proxy
            ? proxy.State
            : throw new SubstituteException($"'{typeof(TInterface).Name}' is not a substitute.");
    }
}
