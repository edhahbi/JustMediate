using Microsoft.Extensions.DependencyInjection;

namespace JustMediate;

internal interface IRequestHandlerBase;

internal interface IRequestHandlerBase<TResponse> : IRequestHandlerBase
{
    internal ValueTask<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken ct);
}

internal sealed class RequestHandlerWrapper<TRequest,TResponse> : IRequestHandlerBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> HandleAsync(
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