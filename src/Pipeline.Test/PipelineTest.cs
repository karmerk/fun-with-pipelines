
using System.Runtime.Intrinsics.X86;

namespace Pipeline.Test;

[TestClass]
public sealed class PipelineTest
{
    private sealed class Step<T> : IPipelineStep<T>
    {
        public static int MaxCallStackCount { get; set; }
        public static int CallStackCount
        {
            get => field; 
            set
            {
                field = value;
                MaxCallStackCount = int.Max(MaxCallStackCount, value);
            }
        }

        public int CallIndex { get; set; }

        public async Task RunAsync(T item, NextStepDelegate next, CancellationToken token)
        {
            CallIndex = CallStackCount;
            CallStackCount++;

            await next();

            CallStackCount--;
        }
    }

    [TestMethod]
    public async Task RunAsync_StepsAreRunInOrder()
    {
        var step1 = new Step<int>();
        var step2 = new Step<int>();
        var step3 = new Step<int>();

        var pipeline = new Pipeline.Pipeline<int>(step1, step2, step3);

        await pipeline.RunAsync(1337, (item, cancellationToken) =>
        {
            Assert.AreEqual(1337, item);
            Assert.AreEqual(CancellationToken.None, cancellationToken);

            Assert.AreEqual(3, Step<int>.CallStackCount);
            Assert.AreEqual(3, Step<int>.MaxCallStackCount);

            return Task.CompletedTask;

        }, CancellationToken.None);

        Assert.AreEqual(3, Step<int>.MaxCallStackCount);
        Assert.AreEqual(0, Step<int>.CallStackCount);

        Assert.AreEqual(0, step1.CallIndex);
        Assert.AreEqual(1, step2.CallIndex);
        Assert.AreEqual(2, step3.CallIndex);
    }

    [TestMethod]
    public async Task RunAsync_NoSteps_IsQuiteBoring()
    {
        var pipeline = new Pipeline.Pipeline<int>([]);

        await pipeline.RunAsync(1337, (item, cancellationToken) => Task.CompletedTask, CancellationToken.None);
    }
}
