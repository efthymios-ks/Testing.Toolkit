namespace Testing.Toolkit.Substitutes.Internal;

/// <summary>
/// A matcher is written as an argument (<c>repo.Get(Arg.Any&lt;int&gt;())</c>), so it is evaluated
/// before the call it belongs to. Each one parks here until that call arrives and claims them.
/// </summary>
internal static class ArgumentMatchers
{
    [ThreadStatic]
    private static List<ArgumentMatcher>? _pending;

    public static void Push(ArgumentMatcher matcher)
        => (_pending ??= []).Add(matcher);

    /// <summary>
    /// Null when the call used plain values. Matchers are consumed, so they cannot leak into the
    /// next call.
    /// </summary>
    public static ArgumentMatcher?[]? Take(int argumentCount)
    {
        if (_pending is null || _pending.Count == 0)
        {
            return null;
        }

        var pending = _pending;
        _pending = null;

        var matchers = new ArgumentMatcher?[argumentCount];

        // Matchers are pushed left to right, but only for the arguments that used one. Any
        // shortfall belongs to the leading arguments, which were passed as plain values.
        var offset = argumentCount - pending.Count;

        for (var index = 0; index < pending.Count; index++)
        {
            var target = offset + index;

            if (target >= 0 && target < argumentCount)
            {
                matchers[target] = pending[index];
            }
        }

        return matchers;
    }

    public static void Clear()
        => _pending = null;
}
