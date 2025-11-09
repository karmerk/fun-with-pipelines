using System.Collections.Immutable;

namespace Pipeline;

public interface IPipeline<T>
{
    Task RunAsync(T item, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);

    // TODO: I would like to support results also - We need a return chain that supports a result
    // Yet im not entirely sure if i want the pipeline steps to have access to the result. Im like caught in the mindset that
    // pipeline steps should not modify the response, yet it seems like a obivuals use case. 
    //Task<TResult> RunAsync<TResult>(T item, Func<T, CancellationToken, Task<TResult>> handler, CancellationToken cancellationToken = default);
}


public class Pipeline<T> : IPipeline<T>
{
    private readonly ImmutableArray<IPipelineStep<T>> _steps;
    private T? _item;
    private Func<T, CancellationToken, Task>? _handler;
    private int _index;
    private CancellationToken _cancellationToken;

    public Pipeline(PipelineStepBuilder builder, IServiceProvider serviceProvider)
    {
        _steps = builder.CreateSteps<T>(serviceProvider).ToImmutableArray();
    }

    public async Task RunAsync(T item, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(handler);

        _item = item;
        _handler = handler;
        _cancellationToken = cancellationToken;
        _index = -1;

        await NextAsync();
    }

    private async Task NextAsync()
    {
        _index++;

        var count = _steps.Length;
        if (_index < count)
        {
            var step = _steps[_index];

            await step.RunAsync(_item!, NextAsync, _cancellationToken);

            return;
        }

        // Reached the end
        if (_item is not null && _handler is not null)
        {
            await _handler.Invoke(_item, _cancellationToken);
        }
    }
}
