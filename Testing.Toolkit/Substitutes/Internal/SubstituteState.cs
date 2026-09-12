using System.Reflection;

namespace Testing.Toolkit.Substitutes.Internal;

/// <summary>Everything one substitute remembers: what it was told to do, and what it was asked.</summary>
internal sealed class SubstituteState
{
    private readonly List<CallRule> _rules = [];
    private readonly List<RecordedCall> _calls = [];

    public IReadOnlyList<RecordedCall> Calls
        => _calls;

    public object? Invoke(MethodInfo method, object?[] arguments)
    {
        var matchers = ArgumentMatchers.Take(arguments.Length);
        var specification = new CallSpecification(method, arguments, matchers);

        if (VerificationContext.TakeFor(this) is { } expectation)
        {
            Verify(specification, expectation);

            return DefaultFor(method.ReturnType);
        }

        _calls.Add(new RecordedCall(specification));
        CallContext.LastCall = new PendingSetup(this, specification);

        // The newest matching rule wins, so a later setup can override an earlier one.
        for (var index = _rules.Count - 1; index >= 0; index--)
        {
            if (_rules[index].Specification.Accepts(method, arguments))
            {
                return _rules[index].Produce(arguments);
            }
        }

        return DefaultFor(method.ReturnType);
    }

    public void AddRule(CallSpecification specification, Func<object?[], object?> result)
    {
        _rules.Add(new CallRule(specification, result));

        // The call that carried the setup was not a real interaction.
        RemoveLastMatching(specification);
    }

    public int CountMatching(CallSpecification specification)
        => _calls.Count(call => specification.Accepts(call.Specification.Method, call.Specification.Arguments));

    private void Verify(CallSpecification specification, VerificationContext.Expectation expectation)
    {
        var actual = CountMatching(specification);
        var expected = expectation.Times;

        var satisfied = expected is null ? actual > 0 : actual == expected;

        if (satisfied)
        {
            return;
        }

        var wanted = expected is null ? "at least once" : $"exactly {expected} time(s)";
        var received = _calls.Count == 0
            ? "no calls were received"
            : "received: " + string.Join(", ", _calls.Select(call => call.Specification.Describe()));

        throw new SubstituteException(
            $"Expected {specification.Describe()} {wanted}, but it was called {actual} time(s). {received}."
        );
    }

    public void ClearCalls()
        => _calls.Clear();

    public void ClearRules()
        => _rules.Clear();

    private void RemoveLastMatching(CallSpecification specification)
    {
        for (var index = _calls.Count - 1; index >= 0; index--)
        {
            if (ReferenceEquals(_calls[index].Specification, specification))
            {
                _calls.RemoveAt(index);

                return;
            }
        }
    }

    /// <summary>
    /// An unset call returns something usable rather than null, so a substitute can be awaited or
    /// enumerated without being told about every member first.
    /// </summary>
    private static object? DefaultFor(Type returnType)
    {
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var result = DefaultFor(returnType.GetGenericArguments()[0]);

            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(returnType.GetGenericArguments()[0])
                .Invoke(null, [result]);
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var argument = returnType.GetGenericArguments()[0];

            return Activator.CreateInstance(returnType, DefaultFor(argument));
        }

        if (returnType == typeof(ValueTask))
        {
            return default(ValueTask);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private sealed record CallRule(CallSpecification Specification, Func<object?[], object?> Result)
    {
        public object? Produce(object?[] arguments)
            => Result(arguments);
    }
}

internal sealed record RecordedCall(CallSpecification Specification);

/// <summary>The call a <c>Returns</c> or <c>Throws</c> written straight after it refers to.</summary>
internal sealed record PendingSetup(SubstituteState State, CallSpecification Specification);
