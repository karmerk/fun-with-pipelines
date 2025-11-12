using Microsoft.Extensions.DependencyInjection;

namespace Pipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipeline(this IServiceCollection services, Action<PipelineConfigurer> configure)
    {

        var builder = new PipelineBuilder();
        var configurer = new PipelineConfigurer(builder);

        configure.Invoke(configurer);

        services.AddSingleton(builder);
        services.AddScoped(typeof(IPipeline<>), typeof(Pipeline<>));

        return services;
    }


    
}

public class PipelineConfigurer
{
    private readonly PipelineBuilder _builder;

    public PipelineConfigurer(PipelineBuilder builder)
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