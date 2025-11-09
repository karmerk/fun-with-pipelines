namespace Pipeline;

// Delegate to call next step in the pipeline
public delegate Task NextStepDelegate();

public interface IPipelineStep<T>
{
    Task RunAsync(T item, NextStepDelegate next, CancellationToken token);
}
