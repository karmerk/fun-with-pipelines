using System.Collections.Immutable;

namespace Pipeline;

public interface IPipeline<T>
{
    Task RunAsync(T item, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);

    Task<TResult> RunAsync<TResult>(T item, Func<T, CancellationToken, Task<TResult>> handler, CancellationToken cancellationToken = default);
}

public static class PipelineExtensions
{
    extension<T>(IPipeline<T> pipeline)
    {
        public Task RunAsync(T item, Func<T, Task> handler)
        {
            return pipeline.RunAsync(item, (T i, CancellationToken ct) => handler(item), CancellationToken.None);
        }
    }
}

public class Pipeline<T> : IPipeline<T>
{
    private readonly ImmutableArray<IPipelineStep<T>> _steps;
    private T? _item;
    private Func<T, CancellationToken, Task>? _handler;
    private int _index;
    private CancellationToken _cancellationToken;

    public Pipeline(PipelineBuilder builder, IServiceProvider serviceProvider)
    {
        _steps = builder.CreatePipelineSteps<T>(serviceProvider).ToImmutableArray();
    }

    public Pipeline(params ImmutableArray<IPipelineStep<T>> steps)
    {
        _steps = steps;
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

    public async Task<TResult> RunAsync<TResult>(T item, Func<T, CancellationToken, Task<TResult>> handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(handler);

        TResult? result = default;

        _item = item;
        // Wrap the handler in an new handler that grabs the result
        _handler = async (T i, CancellationToken ct) =>
        {
            result = await handler.Invoke(i, ct);
        };
        _cancellationToken = cancellationToken;
        _index = -1;

        await NextAsync();

        return result!;
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
