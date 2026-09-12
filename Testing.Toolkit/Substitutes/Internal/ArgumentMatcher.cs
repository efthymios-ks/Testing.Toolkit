namespace Testing.Toolkit.Substitutes.Internal;

internal sealed class ArgumentMatcher(Func<object?, bool> predicate)
{
    public static readonly ArgumentMatcher Any = new(_ => true);

    public bool Matches(object? value)
        => predicate(value);
}
