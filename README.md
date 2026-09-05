# Testing.Toolkit

Three things a test project usually reaches for a package to get: filled-in objects, substitutes for
interfaces, and an `HttpClient` that answers without a network. A demo, not a package — clone it and
copy what is useful.

```
Randomization/Randomizer.cs          Value<T>, Many<T>, Use<T>, Range, String, Int, Bool, Enum
Randomization/RandomizerOptions.cs   CollectionSize, MaxDepth, StringAlphabet, StringLength
Substitutes/Substitute.cs            For<TInterface>()
Substitutes/SubstituteExtensions.cs  Returns, ReturnsInOrder, Throws, When, Received, DidNotReceive
Substitutes/Arg.cs                   Any<T>, Is<T>
Http/MockHttpMessageHandler.cs       response factory, captured requests, CreateClient
Http/CapturedHttpRequest.cs          method, uri, headers, body — read after the client is done
```

## Randomization

```csharp
var randomizer = new Randomizer(seed: 42);

var order = randomizer.Value<Order>();
var orders = randomizer.Many<Order>(10);

var reference = randomizer.String();
var quantity = randomizer.Int(1, 100);
```

A test states the one field it cares about and lets the rest be filled, so adding a property to
`Order` does not touch the test. The same seed fills the same graph, which is what makes a failure
reproducible.

```csharp
var randomizer = new Randomizer(seed: 1)
    .Use(() => new Uri("https://api.test"))
    .Use(CustomerId.New());

randomizer.Options.CollectionSize = 10;
randomizer.Options.ThrowOnUnsupportedType = true;
```

| Option | Does | Default |
| --- | --- | --- |
| `CollectionSize` | items per generated collection | 3 |
| `MaxDepth` | how deep the walk goes before leaving a property at its default | 5 |
| `StringAlphabet` | characters a generated string is built from | letters and digits |
| `StringLength` | length of a generated string | 10 |
| `ThrowOnUnsupportedType` | throw instead of leaving an unbuildable member alone | false |

Bounds are a rule on the type, so they hold wherever it turns up in a graph, nested and nullable
members included:

```csharp
var randomizer = new Randomizer(seed: 1)
    .Range(1, 100)                                          // int:  >= 1 and < 100
    .Range(0m, 1_000m)                                      // decimal
    .Range(DateTime.UnixEpoch, DateTime.UtcNow)             // DateTime, keeps the kind
    .Range(new TimeOnly(9, 0), new TimeOnly(17, 0));        // TimeOnly

var order = randomizer.Value<Order>();   // Total in [0, 1000), PlacedAt before now
```

Overloads take `int`, `long`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, `DateOnly`,
`TimeOnly` and `TimeSpan`. Both bounds read as min inclusive, max exclusive, so `>= 1 && < 100` is
`Range(1, 100)`, `> 0` is `Range(1, …)`, and `<= 100` is `Range(…, 101)`. The newest range for a type
wins, and `Use` and `Range` share one table, so either replaces the other.

`Use` wins over everything, so an interface, an abstract type, or a value with an invariant is
supplied rather than guessed at. Constructor parameters are filled the same way as properties, so a
type with no parameterless constructor still works. A type that references itself stops at
`MaxDepth` instead of recursing forever.

## Substitutes

```csharp
var repository = Substitute.For<IOrderRepository>();

repository.Find(1).Returns(order);
repository.Find(Arg.Any<int>()).Returns(arguments => new Order((int)arguments[0]!));
repository.Count().ReturnsInOrder(1, 2, 3);
repository.Find(404).Throws(new KeyNotFoundException());

repository.When(substitute => substitute.Delete(Arg.Any<int>())).Throws(new UnauthorizedAccessException());

var found = repository.Find(1);

repository.Received().Find(1);
repository.Received(2).Find(Arg.Any<int>());
repository.DidNotReceive().Delete(1);
```

| Call | Does |
| --- | --- |
| `Returns(value)` / `Returns(factory)` | answers the call just written, optionally from its arguments |
| `ReturnsInOrder(a, b)` | walks the values, then repeats the last |
| `Throws(exception)` / `Throws(factory)` | throws instead of returning |
| `When(call).Throws(…)` / `.Do(…)` | configures a member that returns nothing |
| `Received()` / `Received(n)` | asserts the call written next happened, at least once or exactly n times |
| `DidNotReceive()` | asserts it never happened |
| `ReceivedCalls()` | every call, oldest first, as readable signatures |
| `ClearReceivedCalls()` / `ClearSetups()` | forgets the calls, or the setups, not both |
| `Arg.Any<T>()` / `Arg.Is<T>(predicate)` | matches any value, or one the predicate accepts |

Built on `DispatchProxy`, so substitutes are interfaces only and no proxy generator ships with this.
A member that was never set up returns something usable rather than null — a completed `Task`, a
zero, an empty struct — so a substitute can be awaited without being told about every member first.
The newest matching setup wins, so a test can override one written in its fixture.

## HttpClient

```csharp
var handler = MockHttpMessageHandler.RespondWithJson(new Order("A-1"));
using var client = handler.CreateClient("https://api.test");

var response = await client.PostAsJsonAsync("/orders", order);

var sent = handler.LastRequest!;
Assert.Equal("Bearer token", sent.Header("Authorization"));
Assert.Contains("A-1", sent.Content);
```

The factory sees the request, so one handler can answer a whole conversation, and it may throw to
simulate a transport failure:

```csharp
var handler = new MockHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
{
    "/orders" when request.Method == HttpMethod.Post => MockHttpMessageHandler.Respond(HttpStatusCode.Created),
    "/orders" => MockHttpMessageHandler.RespondJson(orders),
    "/down" => throw new HttpRequestException("connection refused"),
    _ => MockHttpMessageHandler.Respond(HttpStatusCode.NotFound)
});
```

| Member | Does |
| --- | --- |
| `RespondWith(status, content, mediaType)` | same response for every request |
| `RespondWithJson(json)` / `RespondWithJson(body)` | same JSON response, serialized per call |
| `Respond(...)` / `RespondJson(...)` | one response, for building an answer inside a factory |
| `ThrowsWith(exception)` | fails every request the way a transport error would |
| `CreateClient(baseAddress)` | an `HttpClient` on this handler, which it does not own |
| `Requests` / `LastRequest` / `CallCount` | what was sent, oldest first |
| `ClearRequests()` | forgets what was captured |

`HttpClient` disposes a request and its content as soon as the call returns, so each one is copied
into a `CapturedHttpRequest` on the way through — method, uri, version, request and content headers,
and the body as bytes. A request is captured even when the factory throws, and the assertions below
still read after the client is disposed.

| On a captured request | Does |
| --- | --- |
| `Header(name)` | first value, looked up on the request then the content, case-insensitive |
| `Headers` / `ContentHeaders` | every value, kept apart because they live in different places |
| `Content` / `ContentBytes` / `HasContent` | the body, as text or as a copy of the bytes |

A response is built fresh per call. Handing the same `HttpResponseMessage` to two calls fails on the
second read, because the first one disposed its content stream.

## License

MIT.
