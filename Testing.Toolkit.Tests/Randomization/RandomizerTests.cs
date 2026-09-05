using Testing.Toolkit.Randomization;

namespace Testing.Toolkit.Tests.Randomization;

public sealed class RandomizerTests
{
    [Fact]
    public void Value_WhenTypeIsString_ShouldReturnNonEmpty()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 1);

        // Act
        var value = randomizer.Value<string>();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(value));
    }

    [Fact]
    public void Value_WhenSeedIsTheSame_ShouldProduceTheSameGraph()
    {
        // Arrange & Act
        var first = new Randomizer(seed: 42).Value<Order>();
        var second = new Randomizer(seed: 42).Value<Order>();

        // Assert
        Assert.Equal(first.Reference, second.Reference);
        Assert.Equal(first.Total, second.Total);
        Assert.Equal(first.Lines.Count, second.Lines.Count);
    }

    [Fact]
    public void Value_WhenSeedDiffers_ShouldProduceADifferentGraph()
    {
        // Arrange & Act
        var first = new Randomizer(seed: 1).Value<Order>();
        var second = new Randomizer(seed: 2).Value<Order>();

        // Assert
        Assert.NotEqual(first.Reference, second.Reference);
    }

    [Fact]
    public void Value_WhenTypeIsComplex_ShouldFillWritableProperties()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 3);

        // Act
        var order = randomizer.Value<Order>();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(order.Reference));
        Assert.NotEqual(0, order.Total);
        Assert.NotEqual(default, order.PlacedAt);
        Assert.NotNull(order.Lines);
    }

    [Fact]
    public void Value_WhenPropertyIsReadOnly_ShouldLeaveItAlone()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 4);

        // Act
        var order = randomizer.Value<Order>();

        // Assert
        Assert.Equal("read-only", order.Constant);
    }

    [Fact]
    public void Value_WhenTypeHasOnlyAConstructor_ShouldFillItsParameters()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 5);

        // Act
        var customer = randomizer.Value<Customer>();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(customer.Name));
        Assert.NotEqual(Guid.Empty, customer.Id);
    }

    [Fact]
    public void Value_WhenTypeIsEnum_ShouldReturnADeclaredValue()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 6);

        // Act
        var statuses = randomizer.Many<OrderStatus>(20);

        // Assert
        Assert.All(statuses, status => Assert.True(System.Enum.IsDefined(status)));
    }

    [Fact]
    public void Value_WhenTypeIsNullable_ShouldReturnAValue()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 7);

        // Act
        var value = randomizer.Value<int?>();

        // Assert
        Assert.NotNull(value);
    }

    [Theory]
    [InlineData(typeof(int[]))]
    [InlineData(typeof(List<string>))]
    [InlineData(typeof(IEnumerable<int>))]
    [InlineData(typeof(IReadOnlyList<string>))]
    [InlineData(typeof(ICollection<int>))]
    public void Value_WhenTypeIsACollection_ShouldFillItToTheConfiguredSize(Type type)
    {
        // Arrange
        var randomizer = new Randomizer(seed: 8);
        randomizer.Options.CollectionSize = 4;

        // Act
        var value = randomizer.Value(type);

        // Assert
        var items = Assert.IsAssignableFrom<System.Collections.IEnumerable>(value);

        Assert.Equal(4, items.Cast<object>().Count());
    }

    [Fact]
    public void Value_WhenTypeIsADictionary_ShouldFillItToTheConfiguredSize()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 9);
        randomizer.Options.CollectionSize = 2;

        // Act
        var value = randomizer.Value<Dictionary<string, int>>();

        // Assert
        Assert.Equal(2, value.Count);
    }

    [Fact]
    public void Value_WhenTypeReferencesItself_ShouldStopAtMaxDepth()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 10);
        randomizer.Options.MaxDepth = 2;

        // Act
        var node = randomizer.Value<Node>();

        // Assert
        Assert.NotNull(node.Child);
        Assert.Null(node.Child.Child);
    }

    [Fact]
    public void Value_WhenAFactoryIsRegistered_ShouldUseIt()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 11).Use("fixed");

        // Act
        var order = randomizer.Value<Order>();

        // Assert
        Assert.Equal("fixed", order.Reference);
    }

    [Fact]
    public void Value_WhenTypeIsAbstractAndThrowIsOff_ShouldReturnNull()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 12);

        // Act
        var value = randomizer.Value(typeof(Stream));

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void Value_WhenTypeIsAbstractAndThrowIsOn_ShouldThrow()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 13);
        randomizer.Options.ThrowOnUnsupportedType = true;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => randomizer.Value(typeof(Stream)));
    }

    [Fact]
    public void Value_WhenTypeIsNull_ShouldThrow()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 14);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => randomizer.Value(null!));
    }

    [Fact]
    public void Many_WhenCountIsGiven_ShouldReturnThatMany()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 15);

        // Act
        var values = randomizer.Many<int>(7);

        // Assert
        Assert.Equal(7, values.Count);
    }

    [Fact]
    public void Many_WhenCountIsOmitted_ShouldUseTheConfiguredCollectionSize()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 16);
        randomizer.Options.CollectionSize = 5;

        // Act
        var values = randomizer.Many<int>();

        // Assert
        Assert.Equal(5, values.Count);
    }

    [Fact]
    public void Many_WhenCountIsNegative_ShouldThrow()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 17);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Many<int>(-1));
    }

    [Fact]
    public void String_WhenAlphabetIsRestricted_ShouldOnlyUseThoseCharacters()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 18);
        randomizer.Options.StringAlphabet = "ab";
        randomizer.Options.StringLength = 32;

        // Act
        var value = randomizer.String();

        // Assert
        Assert.Equal(32, value.Length);
        Assert.All(value, character => Assert.Contains(character, "ab"));
    }

    [Fact]
    public void Int_WhenRangeIsGiven_ShouldStayInsideIt()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 19);

        // Act
        var values = Enumerable.Range(0, 50).Select(_ => randomizer.Int(10, 20));

        // Assert
        Assert.All(values, value => Assert.InRange(value, 10, 19));
    }

    [Fact]
    public void Enum_WhenCalled_ShouldReturnADeclaredValue()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 20);

        // Act
        var status = randomizer.Enum<OrderStatus>();

        // Assert
        Assert.True(System.Enum.IsDefined(status));
    }

    [Fact]
    public void Use_WhenAValueIsGiven_ShouldReturnThatValue()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 21).Use(new Customer(Guid.Empty, "pinned"));

        // Act
        var customer = randomizer.Value<Customer>();

        // Assert
        Assert.Equal("pinned", customer.Name);
    }

    [Fact]
    public void Use_WhenFactoryIsNull_ShouldThrow()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 22);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => randomizer.Use((Func<int>)null!));
    }

    [Fact]
    public void Seed_WhenNotGiven_ShouldStillBeReadable()
    {
        // Arrange & Act
        var randomizer = new Randomizer();

        // Assert
        Assert.Equal(randomizer.Seed, new Randomizer(randomizer.Seed).Seed);
    }

    [Fact]
    public void CollectionSize_WhenNegative_ShouldThrow()
    {
        // Arrange
        var options = new RandomizerOptions();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.CollectionSize = -1);
    }

    [Fact]
    public void MaxDepth_WhenZero_ShouldThrow()
    {
        // Arrange
        var options = new RandomizerOptions();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = 0);
    }

    [Fact]
    public void Bool_WhenCalledRepeatedly_ShouldReturnBothValues()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 23);

        // Act
        var values = Enumerable.Range(0, 50).Select(_ => randomizer.Bool()).ToArray();

        // Assert
        Assert.Contains(true, values);
        Assert.Contains(false, values);
    }

    [Fact]
    public void Range_WhenIntBoundsAreGiven_ShouldStayInsideThem()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 24).Range(10, 20);

        // Act
        var values = randomizer.Many<int>(50);

        // Assert
        Assert.All(values, value => Assert.InRange(value, 10, 19));
    }

    [Fact]
    public void Range_WhenLongBoundsAreGiven_ShouldStayInsideThem()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 25).Range(10L, 20L);

        // Act
        var values = randomizer.Many<long>(50);

        // Assert
        Assert.All(values, value => Assert.InRange(value, 10L, 19L));
    }

    [Fact]
    public void Range_WhenDoubleBoundsAreGiven_ShouldStayInsideThem()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 26).Range(1.5, 2.5);

        // Act
        var values = randomizer.Many<double>(50);

        // Assert
        Assert.All(values, value => Assert.InRange(value, 1.5, 2.5));
    }

    [Fact]
    public void Range_WhenDecimalBoundsAreGiven_ShouldStayInsideThem()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 27).Range(0m, 100m);

        // Act
        var values = randomizer.Many<decimal>(50);

        // Assert
        Assert.All(values, value => Assert.InRange(value, 0m, 100m));
    }

    [Fact]
    public void Range_WhenDateTimeBoundsAreGiven_ShouldStayInsideThemAndKeepTheKind()
    {
        // Arrange
        var from = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var randomizer = new Randomizer(seed: 28).Range(from, to);

        // Act
        var values = randomizer.Many<DateTime>(50);

        // Assert
        Assert.All(values, value =>
        {
            Assert.InRange(value, from, to);
            Assert.Equal(DateTimeKind.Utc, value.Kind);
        });
    }

    [Fact]
    public void Range_WhenDateTimeOffsetBoundsAreGiven_ShouldStayInsideThemAndKeepTheOffset()
    {
        // Arrange
        var offset = TimeSpan.FromHours(2);
        var from = new DateTimeOffset(2020, 1, 1, 0, 0, 0, offset);
        var to = new DateTimeOffset(2021, 1, 1, 0, 0, 0, offset);
        var randomizer = new Randomizer(seed: 29).Range(from, to);

        // Act
        var values = randomizer.Many<DateTimeOffset>(50);

        // Assert
        Assert.All(values, value =>
        {
            Assert.InRange(value, from, to);
            Assert.Equal(offset, value.Offset);
        });
    }

    [Fact]
    public void Range_WhenDateOnlyBoundsAreGiven_ShouldStayInsideThem()
    {
        // Arrange
        var from = new DateOnly(2020, 1, 1);
        var to = new DateOnly(2020, 2, 1);
        var randomizer = new Randomizer(seed: 30).Range(from, to);

        // Act
        var values = randomizer.Many<DateOnly>(50);

        // Assert
        Assert.All(values, value => Assert.InRange(value, from, to.AddDays(-1)));
    }

    [Fact]
    public void Range_WhenTimeOnlyBoundsAreGiven_ShouldStayInsideThem()
    {
        // Arrange
        var from = new TimeOnly(9, 0);
        var to = new TimeOnly(17, 0);
        var randomizer = new Randomizer(seed: 31).Range(from, to);

        // Act
        var values = randomizer.Many<TimeOnly>(50);

        // Assert
        Assert.All(values, value => Assert.InRange(value, from, to));
    }

    [Fact]
    public void Range_WhenTimeSpanBoundsAreGiven_ShouldStayInsideThem()
    {
        // Arrange
        var from = TimeSpan.FromMinutes(1);
        var to = TimeSpan.FromMinutes(5);
        var randomizer = new Randomizer(seed: 32).Range(from, to);

        // Act
        var values = randomizer.Many<TimeSpan>(50);

        // Assert
        Assert.All(values, value => Assert.InRange(value, from, to));
    }

    [Fact]
    public void Range_WhenTypeAppearsInAGraph_ShouldApplyToNestedProperties()
    {
        // Arrange
        var from = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var randomizer = new Randomizer(seed: 33)
            .Range(0m, 100m)
            .Range(from, to);

        // Act
        var order = randomizer.Value<Order>();

        // Assert
        Assert.InRange(order.Total, 0m, 100m);
        Assert.InRange(order.PlacedAt, from, to);
    }

    [Fact]
    public void Range_WhenTypeIsNullable_ShouldStillApply()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 34).Range(10, 20);

        // Act
        var value = randomizer.Value<int?>();

        // Assert
        Assert.InRange(value!.Value, 10, 19);
    }

    [Fact]
    public void Range_WhenMinIsNotBelowMax_ShouldThrow()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 35);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(10L, 9L));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(1.5, 1.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(1m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(DateTime.MaxValue, DateTime.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => randomizer.Range(DateTimeOffset.MaxValue, DateTimeOffset.MinValue)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(DateOnly.MaxValue, DateOnly.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(TimeOnly.MaxValue, TimeOnly.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.Range(TimeSpan.MaxValue, TimeSpan.MinValue));
    }

    [Fact]
    public void Range_WhenSetTwiceForTheSameType_ShouldUseTheNewest()
    {
        // Arrange
        var randomizer = new Randomizer(seed: 36)
            .Range(0, 5)
            .Range(100, 200);

        // Act
        var values = randomizer.Many<int>(20);

        // Assert
        Assert.All(values, value => Assert.InRange(value, 100, 199));
    }

    private sealed class Order
    {
        public string Reference { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public DateTimeOffset PlacedAt { get; set; }

        public OrderStatus Status { get; set; }

        public IReadOnlyList<string> Lines { get; set; } = [];

        public string Constant { get; } = "read-only";
    }

    private sealed class Customer(Guid id, string name)
    {
        public Guid Id { get; } = id;

        public string Name { get; } = name;
    }

    private sealed class Node
    {
        public Node? Child { get; set; }
    }

    private enum OrderStatus
    {
        Pending,
        Shipped,
        Cancelled
    }
}
