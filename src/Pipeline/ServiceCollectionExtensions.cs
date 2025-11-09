using Microsoft.Extensions.DependencyInjection;

namespace Pipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipeline(this IServiceCollection services, Action<PipelineConfigurer> configure)
    {
        var dictionary = new OrderedDictionary<Type, List<Type>>();
        var configurer = new PipelineConfigurer(services, dictionary);

        configure.Invoke(configurer);

        var builder = new PipelineStepBuilder(dictionary);

        services.AddSingleton(builder);

        services.AddScoped(typeof(IPipeline<>), typeof(Pipeline<>));

        // TODO now we have the steps.. we need to feed them into the service collection

        //var @interface = typeof(IPipelineStep<object>).GetGenericTypeDefinition();

        //foreach(var (target,steps) in dictionary)
        //{
        //    if (target.IsGenericTypeParameter is false)
        //    {
        //        var serviceType = @interface.MakeGenericType(target);

        //        foreach (var implementationType in steps)
        //        {
        //            services.AddScoped(serviceType, implementationType);
        //        }

        //        services.AddScoped(typeof(Pipeline<object>).GetGenericTypeDefinition().MakeGenericType(target));
        //    }
        //}

        return services;
    }


    
}

public class PipelineConfigurer
{
    private readonly IServiceCollection _services;
    private readonly OrderedDictionary<Type, List<Type>> _dictionary;

    public PipelineConfigurer(IServiceCollection services, OrderedDictionary<Type, List<Type>> dictionary)
    {
        _services = services;
        _dictionary = dictionary;
    }

    public PipelineConfigurer WithStep<TStep>()
    {
        return WithStep(typeof(TStep));
    }

    public PipelineConfigurer WithStep(Type stepType)
    {
        var @interfaces = stepType.GetInterfaces().Where(x => x.Name == "IPipelineStep`1" && x.Namespace == "Piper");

        foreach (var @interface in @interfaces)
        {
            var target = @interface.GetGenericArguments().FirstOrDefault();
            if (target is not null)
            {
                if (!_dictionary.TryGetValue(target, out var steps))
                {
                    _dictionary[target] = steps = new List<Type>();
                }

                steps.Add(stepType);
            }
        }

        return this;
    }

    public PipelineConfigurer WithBeforeAction(Delegate action)
    {
        // TODO Grab parameters from action and make wrapper class for this use case.
        return this;
    }
}