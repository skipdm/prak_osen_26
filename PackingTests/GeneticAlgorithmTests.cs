using System.Diagnostics;
using PackingLib;

namespace PackingTests;

public class GeneticAlgorithmTests
{
    [Fact]
    public void ParallelIsFasterThanSequential()
    {
        MeasureTime(true, 2);
        MeasureTime(false, 2);

        long sequentialTime = MeasureTime(false, 20);
        long parallelTime = MeasureTime(true, 20);

        Assert.True(parallelTime < sequentialTime);
    }

    [Fact]
    public void KnownInputGivesOptimalArea()
    {
        GeneticAlgorithm ga = new GeneticAlgorithm(10, 6, 4, 1000, 0.1);

        for (int i = 0; i < 500; i++)
            ga.NextGeneration();

        Assert.Equal(70, ga.Best.Area);
    }

    [Fact]
    public void ResidualDecreasesOnRandomInput()
    {
        Random random = new Random();
        int count1 = random.Next(5, 21);
        int count2 = random.Next(3, 11);
        int count3 = random.Next(2, 7);
        int totalArea = count1 + 4 * count2 + 9 * count3;

        GeneticAlgorithm ga = new GeneticAlgorithm(count1, count2, count3, 500, 0.1);
        int firstResidual = ga.Best.Area - totalArea;
        int previousResidual = firstResidual;

        for (int step = 0; step < 10; step++)
        {
            for (int i = 0; i < 100; i++)
                ga.NextGeneration();

            int residual = ga.Best.Area - totalArea;
            Assert.True(residual >= 0);
            Assert.True(residual <= previousResidual);
            previousResidual = residual;
        }

        Assert.True(previousResidual < firstResidual / 4);
    }

    private long MeasureTime(bool useParallel, int generations)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        GeneticAlgorithm ga = new GeneticAlgorithm(20, 20, 20, 2000, 0.1, useParallel);
        for (int i = 0; i < generations; i++)
            ga.NextGeneration();

        return stopwatch.ElapsedMilliseconds;
    }
}
