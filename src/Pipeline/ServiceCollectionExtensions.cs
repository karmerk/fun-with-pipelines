using Microsoft.Extensions.DependencyInjection;

namespace Pipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipeline(this IServiceCollection services, Action<PipelineConfigurer> configure)
    {

        var builder = new PipelineStepBuilder();
        var configurer = new PipelineConfigurer(builder);

        configure.Invoke(configurer);

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
    private readonly PipelineStepBuilder _builder;

    public PipelineConfigurer(PipelineStepBuilder builder)
    {
        _builder = builder;
    }

    public PipelineConfigurer WithStep<TStep>()
    {
        _builder.AddPipelineStep(typeof(TStep));

        return this;
    }

    public PipelineConfigurer WithStep(Type stepType)
    {
        _builder.AddPipelineStep(stepType);

        return this;
    }

    public PipelineConfigurer WithBeforeAction(Delegate action)
    {
        // TODO Grab parameters from action and make wrapper class for this use case.
        return this;
    }
}