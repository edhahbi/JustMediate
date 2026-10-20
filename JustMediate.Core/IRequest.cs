namespace JustMediate;

public interface IRequest<TResponse>;
public interface IRequest : IRequest<Unit>;
public readonly struct Unit
{
    public static Unit Value => default;
}