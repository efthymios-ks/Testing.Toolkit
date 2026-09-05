using System.Reflection;

namespace Testing.Toolkit.Substitutes.Internal;

/// <summary>
/// Built on DispatchProxy, which the runtime provides. That limits substitutes to interfaces, and
/// buys not carrying a proxy generator of our own.
/// </summary>
internal class SubstituteProxy : DispatchProxy
{
    public SubstituteState State { get; private set; } = null!;

    public static object Create(Type interfaceType, SubstituteState state)
    {
        if (!interfaceType.IsInterface)
        {
            throw new SubstituteException($"'{interfaceType.Name}' is not an interface; only interfaces can be substituted.");
        }

        var proxy = Create(interfaceType, typeof(SubstituteProxy));
        ((SubstituteProxy)proxy!).State = state;

        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        => targetMethod is null
            ? null
            : State.Invoke(targetMethod, args ?? []);
}
