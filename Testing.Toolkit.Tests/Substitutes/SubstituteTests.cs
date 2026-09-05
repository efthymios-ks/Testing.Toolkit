using Testing.Toolkit.Substitutes;

namespace Testing.Toolkit.Tests.Substitutes;

public sealed class SubstituteTests
{
    [Fact]
    public void For_WhenMemberIsNotConfigured_ShouldReturnDefault()
    {
        // Arrange & Act
        var repository = Substitute.For<IOrderRepository>();

        // Assert
        Assert.Null(repository.Find(1));
        Assert.Equal(0, repository.Count());
    }

    [Fact]
    public async Task For_WhenMemberReturnsTask_ShouldReturnCompletedTask()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        await repository.SaveAsync("a");
    }

    [Fact]
    public async Task For_WhenMemberReturnsTaskOfValue_ShouldReturnDefaultResult()
    {
        // Arrange & Act
        var repository = Substitute.For<IOrderRepository>();

        // Assert
        Assert.Null(await repository.FindAsync(1));
    }

    [Fact]
    public async Task For_WhenMemberReturnsValueTaskOfValue_ShouldReturnDefaultResult()
    {
        // Arrange & Act
        var repository = Substitute.For<IOrderRepository>();

        // Assert
        Assert.Equal(0, await repository.CountAsync());
    }

    [Fact]
    public void For_WhenTypeIsNotAnInterface_ShouldThrow()
    {
        // Act & Assert
        var error = Assert.Throws<SubstituteException>(Substitute.For<OrderRepository>);

        Assert.Contains("only interfaces", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void For_WhenMemberIsAProperty_ShouldBeConfigurable()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Name.Returns("orders");

        // Assert
        Assert.Equal("orders", repository.Name);
    }

    [Fact]
    public void Returns_WhenArgumentMatches_ShouldReturnTheValue()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1).Returns("first");

        // Assert
        Assert.Equal("first", repository.Find(1));
    }

    [Fact]
    public void Returns_WhenArgumentDiffers_ShouldReturnDefault()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1).Returns("first");

        // Assert
        Assert.Null(repository.Find(2));
    }

    [Fact]
    public void Returns_WhenAnyMatcherIsUsed_ShouldMatchEveryArgument()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(Arg.Any<int>()).Returns("any");

        // Assert
        Assert.Equal("any", repository.Find(7));
        Assert.Equal("any", repository.Find(-1));
    }

    [Fact]
    public void Returns_WhenPredicateMatcherIsUsed_ShouldOnlyMatchAcceptedValues()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(Arg.Is<int>(id => id > 10)).Returns("big");

        // Assert
        Assert.Equal("big", repository.Find(11));
        Assert.Null(repository.Find(9));
    }

    [Fact]
    public void Returns_WhenOnlyOneArgumentUsesAMatcher_ShouldCompareTheOtherByValue()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Search("open", Arg.Any<int>()).Returns("found");

        // Assert
        Assert.Equal("found", repository.Search("open", 5));
        Assert.Null(repository.Search("closed", 5));
    }

    [Fact]
    public void Returns_WhenFactoryIsGiven_ShouldComputeFromTheArguments()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(Arg.Any<int>()).Returns(arguments => $"order-{arguments[0]}");

        // Assert
        Assert.Equal("order-3", repository.Find(3));
    }

    [Fact]
    public void Returns_WhenConfiguredTwice_ShouldUseTheNewest()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(Arg.Any<int>()).Returns("old");
        repository.Find(Arg.Any<int>()).Returns("new");

        // Assert
        Assert.Equal("new", repository.Find(1));
    }

    [Fact]
    public void Returns_WhenSetupFollowsNoCall_ShouldThrow()
    {
        // Arrange
        // The parked call is thread-static, so a fresh thread is the only place with none parked.
        Exception? captured = null;

        // Act & Assert
        var thread = new Thread(() => captured = Record.Exception(() => "loose".Returns("value")));
        thread.Start();
        thread.Join();

        Assert.IsType<SubstituteException>(captured);
    }

    [Fact]
    public async Task Returns_WhenMemberIsAsync_ShouldReturnTheValue()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.FindAsync(1).Returns(Task.FromResult<string?>("first"));

        // Assert
        Assert.Equal("first", await repository.FindAsync(1));
    }

    [Fact]
    public void ReturnsInOrder_WhenCalledRepeatedly_ShouldWalkTheValuesThenRepeatTheLast()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Count().ReturnsInOrder(1, 2);

        // Assert
        Assert.Equal(1, repository.Count());
        Assert.Equal(2, repository.Count());
        Assert.Equal(2, repository.Count());
    }

    [Fact]
    public void ReturnsInOrder_WhenNoValuesAreGiven_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act & Assert
        Assert.Throws<SubstituteException>(() => repository.Count().ReturnsInOrder());
    }

    [Fact]
    public void Throws_WhenTheCallIsMade_ShouldThrowTheException()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();
        var expected = new InvalidOperationException("no");

        repository.Find(1).Throws(expected);

        // Act & Assert
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => repository.Find(1)));
    }

    [Fact]
    public void Throws_WhenFactoryIsGiven_ShouldBuildTheExceptionPerCall()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository.Find(Arg.Any<int>()).Throws(arguments => new ArgumentException($"bad {arguments[0]}"));

        // Act & Assert
        var error = Assert.Throws<ArgumentException>(() => repository.Find(4));

        Assert.Contains("bad 4", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void When_WhenMemberReturnsNothing_ShouldStillBeAbleToThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();
        var expected = new InvalidOperationException("no");

        repository.When(substitute => substitute.Delete(1)).Throws(expected);

        // Act & Assert
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => repository.Delete(1)));
        repository.Delete(2);
    }

    [Fact]
    public void When_WhenDoIsGiven_ShouldRunItWithTheArguments()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();
        var deleted = new List<object?>();

        repository.When(substitute => substitute.Delete(Arg.Any<int>())).Do(arguments => deleted.Add(arguments[0]));

        // Act
        repository.Delete(1);
        repository.Delete(2);

        // Assert
        Assert.Equal([1, 2], deleted);
    }

    [Fact]
    public void When_WhenCallIsNull_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => repository.When(null!));
    }

    [Fact]
    public void When_WhenThrowsFactoryIsGiven_ShouldBuildTheExceptionPerCall()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository
            .When(substitute => substitute.Delete(Arg.Any<int>()))
            .Throws(arguments => new ArgumentException($"bad {arguments[0]}"));

        // Act & Assert
        var error = Assert.Throws<ArgumentException>(() => repository.Delete(4));

        Assert.Contains("bad 4", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void When_WhenSetupIsNull_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();
        var setup = repository.When(substitute => substitute.Delete(1));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => setup.Throws((Exception)null!));
        Assert.Throws<ArgumentNullException>(() => setup.Throws((Func<object?[], Exception>)null!));
        Assert.Throws<ArgumentNullException>(() => setup.Do(null!));
    }

    [Fact]
    public void Is_WhenPredicateIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Arg.Is<int>(null!));
    }

    [Fact]
    public void Returns_WhenFactoryIsNull_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => repository.Find(1).Returns((Func<object?[], string?>)null!));
    }

    [Fact]
    public void Throws_WhenExceptionIsNull_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => repository.Find(1).Throws((Exception)null!));
    }

    [Fact]
    public void Received_WhenTheCallWasMade_ShouldNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1);

        // Assert
        repository.Received().Find(1);
    }

    [Fact]
    public void Received_WhenTheCallWasNotMade_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act & Assert
        Assert.Throws<SubstituteException>(() => repository.Received().Find(1));
    }

    [Fact]
    public void Received_WhenTheArgumentDiffers_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository.Find(1);

        // Act & Assert
        Assert.Throws<SubstituteException>(() => repository.Received().Find(2));
    }

    [Fact]
    public void Received_WhenTheCountMatches_ShouldNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1);
        repository.Find(1);

        // Assert
        repository.Received(2).Find(1);
    }

    [Fact]
    public void Received_WhenTheCountDiffers_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository.Find(1);

        // Act & Assert
        var error = Assert.Throws<SubstituteException>(() => repository.Received(2).Find(1));

        Assert.Contains("called 1 time(s)", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Received_WhenAMatcherIsUsed_ShouldCountEveryMatchingCall()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1);
        repository.Find(2);

        // Assert
        repository.Received(2).Find(Arg.Any<int>());
    }

    [Fact]
    public void Received_WhenVerifying_ShouldNotRecordTheCall()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1);
        repository.Received().Find(1);

        // Assert
        Assert.Single(repository.ReceivedCalls());
    }

    [Fact]
    public void Received_WhenTheCallIsSetUpToThrow_ShouldNotThrowWhileVerifying()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository.Find(1).Throws(new InvalidOperationException("no"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => repository.Find(1));

        repository.Received().Find(1);
    }

    [Fact]
    public void DidNotReceive_WhenTheCallWasNotMade_ShouldNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1);

        // Assert
        repository.DidNotReceive().Find(2);
    }

    [Fact]
    public void DidNotReceive_WhenTheCallWasMade_ShouldThrow()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository.Find(1);

        // Act & Assert
        Assert.Throws<SubstituteException>(() => repository.DidNotReceive().Find(1));
    }

    [Fact]
    public void ReceivedCalls_WhenCallsWereMade_ShouldDescribeThemOldestFirst()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1);
        repository.Search("open", 2);

        // Assert
        Assert.Equal(["Find(1)", "Search(\"open\", 2)"], repository.ReceivedCalls());
    }

    [Fact]
    public void ReceivedCalls_WhenACallWasOnlyASetup_ShouldNotListIt()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        // Act
        repository.Find(1).Returns("first");

        // Assert
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public void ClearReceivedCalls_WhenCalled_ShouldForgetTheCallsButKeepTheSetups()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository.Find(1).Returns("first");
        repository.Find(1);

        // Act
        repository.ClearReceivedCalls();

        // Assert
        Assert.Empty(repository.ReceivedCalls());
        Assert.Equal("first", repository.Find(1));
    }

    [Fact]
    public void ClearSetups_WhenCalled_ShouldForgetTheSetupsButKeepTheCalls()
    {
        // Arrange
        var repository = Substitute.For<IOrderRepository>();

        repository.Find(1).Returns("first");
        repository.Find(1);

        // Act
        repository.ClearSetups();

        // Assert
        Assert.Null(repository.Find(1));
        Assert.Equal(2, repository.ReceivedCalls().Count);
    }

    [Fact]
    public void ReceivedCalls_WhenTheTargetIsNotASubstitute_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<SubstituteException>(() => new OrderRepository().ReceivedCalls());
    }

    [Fact]
    public void ReceivedCalls_WhenTheTargetIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IOrderRepository)null!).ReceivedCalls());
    }

    [Fact]
    public void For_WhenSubstitutesAreSeparate_ShouldNotShareState()
    {
        // Arrange
        var first = Substitute.For<IOrderRepository>();
        var second = Substitute.For<IOrderRepository>();

        // Act
        first.Find(1).Returns("first");
        first.Find(1);

        // Assert
        Assert.Null(second.Find(1));
        Assert.Equal(["Find(1)"], first.ReceivedCalls());
        Assert.Equal(["Find(1)"], second.ReceivedCalls());
    }

    public interface IOrderRepository
    {
        string Name { get; }

        string? Find(int id);

        string? Search(string status, int take);

        int Count();

        void Delete(int id);

        Task<string?> FindAsync(int id);

        Task SaveAsync(string order);

        ValueTask<int> CountAsync();
    }

    public sealed class OrderRepository : IOrderRepository
    {
        public string Name
            => string.Empty;

        public string? Find(int id)
            => null;

        public string? Search(string status, int take)
            => null;

        public int Count()
            => 0;

        public void Delete(int id)
        {
        }

        public Task<string?> FindAsync(int id)
            => Task.FromResult<string?>(null);

        public Task SaveAsync(string order)
            => Task.CompletedTask;

        public ValueTask<int> CountAsync()
            => ValueTask.FromResult(0);
    }
}
