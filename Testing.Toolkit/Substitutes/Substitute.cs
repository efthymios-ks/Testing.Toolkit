using Testing.Toolkit.Substitutes.Internal;

namespace Testing.Toolkit.Substitutes;

public static class Substitute
{
    /// <summary>
    /// A stand-in for an interface. Every member returns a usable default until told otherwise, and
    /// every call is recorded.
    /// </summary>
    public static TInterface For<TInterface>()
        where TInterface : class
        => (TInterface)SubstituteProxy.Create(typeof(TInterface), new SubstituteState());
}
