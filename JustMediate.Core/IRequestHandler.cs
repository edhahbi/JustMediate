namespace JustMediate;

public interface IRequestHandler<in TRequest,TResponse> 
where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}