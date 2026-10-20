using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace JustMediate.Tests;

public sealed class PipelineTests
{
    [Test]
    public async Task Send_executes_behaviors_in_order_around_handler()
    {
        var events = new List<string>();
        var services = CreateServices(events);
        await using var provider = services.BuildServiceProvider();

        var result = await provider
            .GetRequiredService<ISender>()
            .Send(new GetOrderRequest(), CancellationToken.None);

        Assert.That(result, Is.EqualTo("handled"));
        Assert.That(events, Is.EqualTo(new[]
        {
            "first-before",
            "second-before",
            "handler",
            "second-after",
            "first-after"
        }));
    }

    [Test]
    public async Task Behavior_can_short_circuit_handler()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(events);
        services.AddScoped<IRequestHandler<GetOrderRequest, string>, GetOrderHandler>();
        services.AddScoped<IPipelineBehavior<GetOrderRequest, string>, ShortCircuitBehavior>();
        services.AddDispatcher(Assembly.GetExecutingAssembly());

        await using var provider = services.BuildServiceProvider();
        var result = await provider
            .GetRequiredService<ISender>()
            .Send(new GetOrderRequest(), CancellationToken.None);

        Assert.That(result, Is.EqualTo("cached"));
        Assert.That(events, Is.EqualTo(new[] { "short-circuit" }));
    }

    private static IServiceCollection CreateServices(List<string> events)
    {
        return new ServiceCollection()
            .AddSingleton(events)
            .AddScoped<IRequestHandler<GetOrderRequest, string>, GetOrderHandler>()
            .AddScoped<IPipelineBehavior<GetOrderRequest, string>, FirstBehavior>()
            .AddPipelineBehavior(typeof(SecondBehavior<,>))
            .AddDispatcher(Assembly.GetExecutingAssembly());
    }

    private sealed record GetOrderRequest : IRequest<string>;

    private sealed class GetOrderHandler(List<string> events)
        : IRequestHandler<GetOrderRequest, string>
    {
        public ValueTask<string> HandleAsync(GetOrderRequest request, CancellationToken ct)
        {
            events.Add("handler");
            return ValueTask.FromResult("handled");
        }
    }

    private sealed class FirstBehavior(List<string> events)
        : IPipelineBehavior<GetOrderRequest, string>
    {
        public async ValueTask<string> HandleAsync(
            GetOrderRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken ct)
        {
            events.Add("first-before");
            var result = await next();
            events.Add("first-after");
            return result;
        }
    }

    private sealed class SecondBehavior<TRequest, TResponse>(List<string> events)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            events.Add("second-before");
            var result = await next();
            events.Add("second-after");
            return result;
        }
    }

    private sealed class ShortCircuitBehavior(List<string> events)
        : IPipelineBehavior<GetOrderRequest, string>
    {
        public ValueTask<string> HandleAsync(
            GetOrderRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken ct)
        {
            events.Add("short-circuit");
            return ValueTask.FromResult("cached");
        }
    }
}
