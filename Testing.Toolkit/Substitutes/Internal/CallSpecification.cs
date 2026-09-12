using System.Reflection;

namespace Testing.Toolkit.Substitutes.Internal;

/// <summary>One call that happened, or one that a rule is waiting for.</summary>
internal sealed record CallSpecification(MethodInfo Method, object?[] Arguments, ArgumentMatcher?[]? Matchers)
{
    /// <summary>
    /// Whether this specification accepts the arguments of an actual call. An argument written as a
    /// matcher is asked; anything else is compared by value.
    /// </summary>
    public bool Accepts(MethodInfo method, object?[] arguments)
    {
        if (!Method.Equals(method) || Arguments.Length != arguments.Length)
        {
            return false;
        }

        for (var index = 0; index < arguments.Length; index++)
        {
            var matcher = Matchers?[index];

            var accepted = matcher is null
                ? Equals(Arguments[index], arguments[index])
                : matcher.Matches(arguments[index]);

            if (!accepted)
            {
                return false;
            }
        }

        return true;
    }

    public string Describe()
        => $"{Method.Name}({string.Join(", ", Arguments.Select(Format))})";

    private string Format(object? argument, int index)
        => Matchers?[index] is not null
            ? "<any>"
            : argument switch
            {
                null => "null",
                string text => $"\"{text}\"",
                _ => argument.ToString() ?? "null"
            };
}
