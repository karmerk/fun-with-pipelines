using System.Collections.Immutable;

namespace Pipeline;

/// <summary>
/// Pipeline to handle <c>T</c>
/// </summary>
public interface IPipeline<T>
{
    // TODO: look into converting to ValueTask
    /// <summary>
    /// Run pipeline with <c>T</c> item against a handler
    /// </summary>
    Task RunAsync(T item, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run pipeline with <c>T</c> item against a handler that returns a <c>TResult</c> result
    /// </summary>
    Task<TResult> RunAsync<TResult>(T item, Func<T, CancellationToken, Task<TResult>> handler, CancellationToken cancellationToken = default);
}

public static class PipelineExtensions
{
    extension<T>(IPipeline<T> pipeline)
    {
        public Task RunAsync(T item, Func<T, Task> handler)
        {
            return pipeline.RunAsync(item, (T i, CancellationToken _) => handler(item), CancellationToken.None);
        }

        public Task RunAsync(T item, Action<T> handler)
        {
            return pipeline.RunAsync(item, (T i, CancellationToken _) =>
            {
                handler(item);

                return Task.CompletedTask;
            }, CancellationToken.None);
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
        TResult? result = default;

        await RunAsync(item, async (T i, CancellationToken ct) =>
        {
            result = await handler.Invoke(i, ct);
        }, cancellationToken);

#pragma warning disable CS8603 // Possible null reference return.
        // I guess it could be null if the handler returns null ¯\_(ツ)_/¯
        return result;
#pragma warning restore CS8603 // Possible null reference return.
    }

    private async Task NextAsync()
    {
        _index++;

        // Call next step
        if (_index < _steps.Length)
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
