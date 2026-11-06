namespace PackingLib;

public class GeneticAlgorithm
{
    private readonly GeneticOperations operations;
    private readonly double mutationProbability;
    private readonly bool useParallel;
    private readonly Individual[] population;

    public int[] Sizes => operations.Sizes;
    public int Generation { get; private set; }
    public Individual Best => population[0];

    public GeneticAlgorithm(int count1, int count2, int count3, int populationSize, double mutationProbability, bool useParallel = true)
    {
        this.mutationProbability = mutationProbability;
        this.useParallel = useParallel;

        operations = new GeneticOperations(GeneticOperations.CreateSizes(count1, count2, count3));

        population = new Individual[populationSize];
        if (useParallel)
        {
            Parallel.For(0, populationSize, i => population[i] = operations.CreateRandom());
        }
        else
        {
            for (int i = 0; i < populationSize; i++)
                population[i] = operations.CreateRandom();
        }

        SortPopulation();
    }

    public void NextGeneration()
    {
        int half = population.Length / 2;

        Individual[] parents = new Individual[half];
        Array.Copy(population, parents, half);

        if (useParallel)
        {
            Parallel.For(half, population.Length, i => population[i] = operations.CreateChild(parents, mutationProbability));
        }
        else
        {
            for (int i = half; i < population.Length; i++)
                population[i] = operations.CreateChild(parents, mutationProbability);
        }

        SortPopulation();
        Generation++;
    }

    public void GetBounds(Individual individual, out int minX, out int minY, out int maxX, out int maxY)
    {
        operations.GetBounds(individual, out minX, out minY, out maxX, out maxY);
    }

    private void SortPopulation()
    {
        Array.Sort(population, (a, b) => a.Area.CompareTo(b.Area));
    }
}
