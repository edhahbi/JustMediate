using Microsoft.Extensions.DependencyInjection;

namespace JustMediate;

internal abstract class RequestHandlerBase;

internal abstract class RequestHandlerBase<TResponse> : RequestHandlerBase
{
    internal abstract ValueTask<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken ct);
}

internal sealed class RequestHandlerWrapper<TRequest,TResponse> : RequestHandlerBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    internal override ValueTask<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken ct)
    {
        var typedRequest = (TRequest)request;
        var handler = provider.GetRequiredService<IRequestHandler<TRequest,TResponse>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> pipeline = () => handler.HandleAsync(typedRequest,ct);

        foreach (var behavior in behaviors.Reverse())
        {
            var next = pipeline;
            var current = behavior;
            pipeline = () => current.HandleAsync(typedRequest, next, ct);
        }
        return pipeline();
    }
}