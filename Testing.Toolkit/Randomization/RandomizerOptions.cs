namespace Testing.Toolkit.Randomization;

public sealed class RandomizerOptions
{

    /// <summary>Items generated for a collection property. Defaults to 3.</summary>
    public int CollectionSize
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 3;

    /// <summary>
    /// How deep the generator walks before leaving a property at its default. A graph that
    /// references itself would otherwise never finish. Defaults to 5.
    /// </summary>
    public int MaxDepth
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = 5;

    /// <summary>Characters a generated string is built from.</summary>
    public string StringAlphabet { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public int StringLength { get; set; } = 10;

    /// <summary>
    /// Whether a property the generator cannot build throws or is left alone. Off by default, so a
    /// type with one awkward member still yields a usable object.
    /// </summary>
    public bool ThrowOnUnsupportedType { get; set; }
}
