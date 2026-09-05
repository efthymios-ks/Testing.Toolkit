using Testing.Toolkit.Substitutes.Internal;

namespace Testing.Toolkit.Substitutes;

/// <summary>Argument matchers for setting up and verifying calls.</summary>
public static class Arg
{
    /// <summary>Matches any value, including null.</summary>
    public static TValue Any<TValue>()
    {
        ArgumentMatchers.Push(ArgumentMatcher.Any);

        return default!;
    }

    /// <summary>Matches a value the predicate accepts.</summary>
    public static TValue Is<TValue>(Func<TValue, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        ArgumentMatchers.Push(new ArgumentMatcher(value => value is TValue typed
            ? predicate(typed)
            : value is null && predicate(default!)));

        return default!;
    }
}
