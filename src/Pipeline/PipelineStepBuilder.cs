using Microsoft.Extensions.DependencyInjection;

namespace Pipeline;



public class PipelineStepBuilder
{
    private readonly OrderedDictionary<Type, List<Type>> _dictionary = new OrderedDictionary<Type, List<Type>>();

    public PipelineStepBuilder()
    {
    }

    public PipelineStepBuilder AddPipelineStep<TStep>()
    {
        return AddPipelineStep(typeof(TStep));
    }

    public PipelineStepBuilder AddPipelineStep(Type stepType)
    {
        var @interfaces = stepType.GetInterfaces().Where(x => x.Name == "IPipelineStep`1" && x.Namespace == "Pipeline");

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


    public IEnumerable<IPipelineStep<T>> CreateSteps<T>(IServiceProvider serviceProvider)
    {
        var target = typeof(T);

        foreach (var keyValue in _dictionary)
        {
            var type = keyValue.Key;
            var stepTypes = keyValue.Value;

            // Match on type
            if (type == target)
            {
                foreach (var stepType in stepTypes)
                {
                    var implementation = ActivatorUtilities.CreateInstance(serviceProvider, stepType);
                    var typed = implementation as IPipelineStep<T>;
                    if (typed is not null)
                    {
                        yield return typed;
                    }
                }
            }
            // Match on sub type
            else if (type.IsAssignableFrom(target))
            {
                foreach (var stepType in stepTypes)
                {
                    var implementation = ActivatorUtilities.CreateInstance(serviceProvider, stepType);
                    var wrapperType = typeof(PipelineStepWrapper<,>).MakeGenericType(target, type);
                    var wrapper = ActivatorUtilities.CreateInstance(serviceProvider, wrapperType, implementation);
                    var typed = wrapper as IPipelineStep<T>;
                    if (typed is not null)
                    {
                        yield return typed;
                    }
                }
            }
            // Match on generic ?
            else if (type.IsGenericTypeParameter)
            {
                // TODO: verify constraints?

                foreach (var stepType in stepTypes)
                {
                    var typedStepType = stepType.MakeGenericType(target);

                    var implementation = ActivatorUtilities.CreateInstance(serviceProvider, typedStepType);
                    var typed = implementation as IPipelineStep<T>;
                    if (typed is not null)
                    {
                        yield return typed;
                    }
                }
            }
        }
    }

    
    private sealed class PipelineStepWrapper<T1, T2> : IPipelineStep<T1> where T1 : T2
    {
        private readonly IPipelineStep<T2> _inner;

        public PipelineStepWrapper(IPipelineStep<T2> inner)
        {
            _inner = inner;
        }

        public async Task RunAsync(T1 item, NextStepDelegate next, CancellationToken token)
        {
            var boxed = (T2)item;

            await _inner.RunAsync(boxed, next, token);

        }
    }


}
