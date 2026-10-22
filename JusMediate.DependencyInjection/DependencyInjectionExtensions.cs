using System.Collections.Frozen;
using System.Reflection;
using JustMediate;
using Microsoft.Extensions.DependencyInjection;

namespace JusMedaite.DependencyInjectionAbstractions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDispatcher(this IServiceCollection services, Assembly assembly)
    {
        var requestWrappers = new Dictionary<Type, RequestHandlerBase>();

        // search for all type in the assembly
        foreach (var type in assembly.GetTypes())
        {
            // if it's an interface or abstract class skip it
            if(type.IsInterface || type.IsAbstract) continue;

            foreach (var @interface in type.GetInterfaces())
            {
                // if the implemented interface doesn't take types <> it's not of type IRequestHandler<>
                if(!@interface.IsGenericType) continue;

                var def = @interface.GetGenericTypeDefinition();

                // implemented interface is not of type IRequestHandler 
                if (def != typeof(IRequestHandler<,>)) continue;
                var args = @interface.GetGenericArguments();

                var requestType = args[0];
                var responseType = args[1];

                if (requestWrappers.ContainsKey(requestType)) continue;
                var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
                requestWrappers[requestType] = (RequestHandlerBase)Activator.CreateInstance(wrapperType)!;
            }
        }

        var registry = new DispatcherRegistry(requestWrappers.ToFrozenDictionary());

        services.AddSingleton(registry);
        services.AddScoped<Dispatcher>();
        services.AddScoped<ISender>(provider => provider.GetRequiredService<Dispatcher>());

        return services;
    }

    public static IServiceCollection AddPipelineBehavior (this IServiceCollection services, Type openGenericType)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>),openGenericType);
        return services;
    }
}