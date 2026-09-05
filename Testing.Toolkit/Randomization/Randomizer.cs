using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Testing.Toolkit.Randomization;

/// <summary>
/// Fills an object graph with values so a test can say what it cares about and leave the rest
/// unstated. Seed it to get the same graph every run.
/// </summary>
public sealed class Randomizer
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _writableProperties = new();

    private readonly Random _random;
    private readonly Dictionary<Type, Func<object>> _factories = [];

    public Randomizer()
        : this(Environment.TickCount)
    {
    }

    /// <summary>The same seed produces the same graph, which is what makes a failure reproducible.</summary>
    public Randomizer(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public int Seed { get; }

    public RandomizerOptions Options { get; } = new();

    /// <summary>Supplies values for a type the generator cannot build, or should not guess at.</summary>
    public Randomizer Use<TValue>(Func<TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factories[typeof(TValue)] = () => factory()!;

        return this;
    }

    public Randomizer Use<TValue>(TValue value)
        => Use(() => value);

    /// <summary>
    /// Confines every value of this type to a range, wherever it turns up in a graph. Bounds read as
    /// min inclusive and max exclusive, so <c>&gt;= 1</c> and <c>&lt; 100</c> is <c>Range(1, 100)</c>
    /// and <c>&gt; 0</c> is <c>Range(1, …)</c>.
    /// </summary>
    public Randomizer Range(int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => _random.Next(minInclusive, maxExclusive));
    }

    public Randomizer Range(long minInclusive, long maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => _random.NextInt64(minInclusive, maxExclusive));
    }

    public Randomizer Range(double minInclusive, double maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => minInclusive + (_random.NextDouble() * (maxExclusive - minInclusive)));
    }

    public Randomizer Range(decimal minInclusive, decimal maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => minInclusive + ((decimal)_random.NextDouble() * (maxExclusive - minInclusive)));
    }

    /// <summary>Keeps the kind of the lower bound, so a UTC range stays UTC.</summary>
    public Randomizer Range(DateTime minInclusive, DateTime maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => new DateTime(
            ticks: _random.NextInt64(minInclusive.Ticks, maxExclusive.Ticks),
            kind: minInclusive.Kind
        ));
    }

    /// <summary>Keeps the offset of the lower bound.</summary>
    public Randomizer Range(DateTimeOffset minInclusive, DateTimeOffset maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => new DateTimeOffset(
            ticks: _random.NextInt64(minInclusive.Ticks, maxExclusive.Ticks),
            offset: minInclusive.Offset
        ));
    }

    public Randomizer Range(DateOnly minInclusive, DateOnly maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => DateOnly.FromDayNumber(_random.Next(minInclusive.DayNumber, maxExclusive.DayNumber)));
    }

    public Randomizer Range(TimeOnly minInclusive, TimeOnly maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => new TimeOnly(_random.NextInt64(minInclusive.Ticks, maxExclusive.Ticks)));
    }

    public Randomizer Range(TimeSpan minInclusive, TimeSpan maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        return Use(() => new TimeSpan(_random.NextInt64(minInclusive.Ticks, maxExclusive.Ticks)));
    }

    public TValue Value<TValue>()
        => (TValue)Value(typeof(TValue))!;

    public object? Value(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Create(type, depth: 0);
    }

    public IReadOnlyList<TValue> Many<TValue>(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return [.. Enumerable.Range(0, count).Select(_ => Value<TValue>())];
    }

    public IReadOnlyList<TValue> Many<TValue>()
        => Many<TValue>(Options.CollectionSize);

    public string String()
        => new([.. Enumerable
            .Range(0, Options.StringLength)
            .Select(_ => Options.StringAlphabet[_random.Next(Options.StringAlphabet.Length)])]);

    public int Int(int minInclusive = 0, int maxExclusive = int.MaxValue)
        => _random.Next(minInclusive, maxExclusive);

    public bool Bool()
        => _random.Next(2) == 1;

    /// <summary>One of the enum's declared values, never an unnamed number.</summary>
    public TEnum Enum<TEnum>()
        where TEnum : struct, Enum
    {
        var values = System.Enum.GetValues<TEnum>();

        return values.Length == 0 ? default : values[_random.Next(values.Length)];
    }

    private object? Create(Type type, int depth)
    {
        if (_factories.TryGetValue(type, out var factory))
        {
            return factory();
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return Create(underlying, depth);
        }

        if (TryCreatePrimitive(type, out var primitive))
        {
            return primitive;
        }

        // Past this point every branch recurses, so anything deeper is left at its default.
        if (depth >= Options.MaxDepth)
        {
            return Default(type);
        }

        if (type.IsEnum)
        {
            var values = System.Enum.GetValues(type);

            return values.Length == 0 ? Default(type) : values.GetValue(_random.Next(values.Length));
        }

        if (TryCreateCollection(type, depth, out var collection))
        {
            return collection;
        }

        return CreateComplex(type, depth);
    }

    private bool TryCreatePrimitive(Type type, out object? value)
    {
        value = type switch
        {
            _ when type == typeof(string) => String(),
            _ when type == typeof(bool) => Bool(),
            _ when type == typeof(byte) => (byte)_random.Next(byte.MaxValue + 1),
            _ when type == typeof(sbyte) => (sbyte)_random.Next(sbyte.MinValue, sbyte.MaxValue + 1),
            _ when type == typeof(short) => (short)_random.Next(short.MinValue, short.MaxValue + 1),
            _ when type == typeof(ushort) => (ushort)_random.Next(ushort.MaxValue + 1),
            _ when type == typeof(int) => _random.Next(),
            _ when type == typeof(uint) => (uint)_random.Next(),
            _ when type == typeof(long) => _random.NextInt64(),
            _ when type == typeof(ulong) => (ulong)_random.NextInt64(0, long.MaxValue),
            _ when type == typeof(float) => (float)_random.NextDouble() * 1000,
            _ when type == typeof(double) => _random.NextDouble() * 1000,
            _ when type == typeof(decimal) => (decimal)(_random.NextDouble() * 1000),
            _ when type == typeof(char) => Options.StringAlphabet[_random.Next(Options.StringAlphabet.Length)],
            _ when type == typeof(Guid) => Guid.NewGuid(),
            _ when type == typeof(DateTime) => RandomDateTime(),
            _ when type == typeof(DateTimeOffset) => new DateTimeOffset(RandomDateTime(), TimeSpan.Zero),
            _ when type == typeof(DateOnly) => DateOnly.FromDateTime(RandomDateTime()),
            _ when type == typeof(TimeOnly) => TimeOnly.FromDateTime(RandomDateTime()),
            _ when type == typeof(TimeSpan) => TimeSpan.FromSeconds(_random.Next(86_400)),
            _ when type == typeof(Uri) => new Uri($"https://{String().ToLowerInvariant()}.test"),
            _ when type == typeof(object) => new object(),
            _ => null
        };

        return value is not null;
    }

    private DateTime RandomDateTime()
        => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(_random.Next(int.MaxValue));

    private bool TryCreateCollection(Type type, int depth, out object? collection)
    {
        collection = null;

        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        if (type.IsArray)
        {
            collection = FillArray(type.GetElementType()!, depth);

            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var arguments = type.GetGenericArguments();
        var definition = type.GetGenericTypeDefinition();

        if (arguments.Length == 2 && IsDictionary(definition))
        {
            collection = FillDictionary(arguments[0], arguments[1], depth);

            return collection is not null;
        }

        if (arguments.Length != 1)
        {
            return false;
        }

        var items = FillArray(arguments[0], depth);

        // An interface cannot be constructed; a List of the right element type satisfies all of them.
        if (type.IsInterface)
        {
            collection = definition == typeof(IEnumerable<>)
                || definition == typeof(IReadOnlyCollection<>)
                || definition == typeof(IReadOnlyList<>)
                || definition == typeof(ICollection<>)
                || definition == typeof(IList<>)
                    ? ToList(arguments[0], items)
                    : null;

            return collection is not null;
        }

        collection = TryConstructFrom(type, items);

        return collection is not null;
    }

    private Array FillArray(Type elementType, int depth)
    {
        var items = Array.CreateInstance(elementType, Options.CollectionSize);

        for (var index = 0; index < items.Length; index++)
        {
            items.SetValue(Create(elementType, depth + 1), index);
        }

        return items;
    }

    private object ToList(Type elementType, Array items)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private object? FillDictionary(Type keyType, Type valueType, int depth)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType)!;

        for (var index = 0; index < Options.CollectionSize; index++)
        {
            var key = Create(keyType, depth + 1);

            if (key is not null)
            {
                dictionary[key] = Create(valueType, depth + 1);
            }
        }

        return dictionary;
    }

    private static bool IsDictionary(Type definition)
        => definition == typeof(Dictionary<,>)
            || definition == typeof(IDictionary<,>)
            || definition == typeof(IReadOnlyDictionary<,>);

    private static object? TryConstructFrom(Type type, Array items)
    {
        try
        {
            return Activator.CreateInstance(type, items);
        }
        catch (MissingMethodException)
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch (MissingMethodException)
            {
                return null;
            }
        }
    }

    private object? CreateComplex(Type type, int depth)
    {
        if (type.IsInterface || type.IsAbstract)
        {
            return Unsupported(type);
        }

        var instance = TryConstruct(type, depth);

        if (instance is null)
        {
            return Unsupported(type);
        }

        foreach (var property in WritablePropertiesOf(type))
        {
            var value = Create(property.PropertyType, depth + 1);

            if (value is not null)
            {
                property.SetValue(instance, value);
            }
        }

        return instance;
    }

    /// <summary>
    /// Constructor parameters are filled the same way as properties, so a type with no
    /// parameterless constructor still works.
    /// </summary>
    private object? TryConstruct(Type type, int depth)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        var constructors = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(constructor => constructor.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var arguments = constructor
                .GetParameters()
                .Select(parameter => Create(parameter.ParameterType, depth + 1))
                .ToArray();

            try
            {
                return constructor.Invoke(arguments);
            }
            catch (TargetInvocationException)
            {
                // A constructor that rejects what it was given; try a simpler one.
            }
        }

        return null;
    }

    private object? Unsupported(Type type)
        => Options.ThrowOnUnsupportedType
            ? throw new NotSupportedException($"No value can be generated for '{type.Name}'. Register one with Use.")
            : Default(type);

    private static object? Default(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;

    private static PropertyInfo[] WritablePropertiesOf(Type type)
        => _writableProperties.GetOrAdd(type, static key =>
            [
                .. key
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0)
            ]);
}
