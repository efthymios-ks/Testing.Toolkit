namespace Testing.Toolkit.Substitutes;

/// <summary>A substitute was set up or verified in a way that cannot work.</summary>
public sealed class SubstituteException(string message) : Exception(message);
