using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;

namespace JustMediate;

internal sealed class Dispatcher(IServiceProvider provider, DispatcherRegistry registry) : ISender
{
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        return !registry.RequestWrappers.TryGetValue(request.GetType(), out var wrapper) 
            ? throw new ValidationException($"No handler is registered for request type {request.GetType().FullName}") 
            : ((RequestHandlerBase<TResponse>)wrapper).HandleAsync(request, provider, ct);
    }
}

internal class DispatcherRegistry(FrozenDictionary<Type, RequestHandlerBase> registry)
{
    internal FrozenDictionary<Type, RequestHandlerBase> RequestWrappers { get; } = registry;
}