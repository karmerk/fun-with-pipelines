using Microsoft.Extensions.DependencyInjection;

namespace Pipeline;

public class BeforeAfter<T> : IPipelineStep<T>
{
    public async Task RunAsync(T item, NextStepDelegate next, CancellationToken token)
    {
        Console.WriteLine("Before: " + item);

        await next();

        Console.WriteLine("After: " + item);
    }
}

public class BeforeAfter : IPipelineStep<int>
{
    public async Task RunAsync(int item, NextStepDelegate next, CancellationToken token)
    {
        Console.WriteLine("Before: " + item);

        await next();

        Console.WriteLine("After: " + item);
    }
}



public static class Program
{

    public static async Task HandleAsync(string value, CancellationToken cancellationToken)
    {
        await Task.Yield();


        Console.WriteLine(value);
    }

    public static async Task HandleAsync(int value, CancellationToken cancellationToken)
    {
        await Task.Yield();


        Console.WriteLine(value);
    }


    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddPipeline(configure =>
        {
            configure.WithStep<BeforeAfter<object>>();
            configure.WithStep<BeforeAfter<string>>();
            //configure.WithStep<BeforeAfter>();
            configure.WithStep(typeof(BeforeAfter<>));

            configure.WithBeforeAction((int i, string b) => { });
        });


        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();


        var stringPipeline = scope!.ServiceProvider.GetService<Pipeline<string>>();
        var intPipeline = scope!.ServiceProvider.GetService<Pipeline<int>>();


        //var builder = scope.ServiceProvider.GetRequiredService<PipelineStepBuilder>();


        var pipeline = scope.ServiceProvider.GetRequiredService<IPipeline<string>>();

        // Push the string "Hello" through the pipeline 
        await pipeline.RunAsync("hello", HandleAsync);

        
        var p2 = scope.ServiceProvider.GetRequiredService<IPipeline<int>>();

        // Push the integer 42 through the pipeline 
        await p2.RunAsync(42, HandleAsync);


    }

}
