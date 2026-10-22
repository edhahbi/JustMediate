# JustMediate

JustMediate is a lightweight request/response mediator for .NET with dependency-injection
registration and pipeline behaviors.

## Quick start

### 1. Add JustMediate

From your application directory:

```bash
dotnet add reference ../JustMediate/JustMediate.Core/JustMediate.Core.csproj
```

### 2. Define a request and handler

```csharp
using JustMediate;

public sealed record GetGreeting(string Name) : IRequest<string>;

public sealed class GetGreetingHandler : IRequestHandler<GetGreeting, string>
{
    public ValueTask<string> HandleAsync(GetGreeting request, CancellationToken ct) =>
        ValueTask.FromResult($"Hello, {request.Name}!");
}
```

### 3. Register and send requests

```csharp
using System.Reflection;
using JustMediate;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddScoped<IRequestHandler<GetGreeting, string>, GetGreetingHandler>()
    .AddDispatcher(Assembly.GetExecutingAssembly());

await using var provider = services.BuildServiceProvider();
var sender = provider.GetRequiredService<ISender>();

var greeting = await sender.Send(new GetGreeting("world"), CancellationToken.None);
Console.WriteLine(greeting);
```