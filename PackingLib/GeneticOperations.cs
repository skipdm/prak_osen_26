namespace PackingLib;

public class GeneticOperations
{
    private readonly int[] sizes;
    private readonly int fieldSize;

    public int[] Sizes => sizes;

    public GeneticOperations(int[] sizes)
    {
        this.sizes = sizes;

        int totalArea = 0;
        for (int i = 0; i < sizes.Length; i++)
            totalArea += sizes[i] * sizes[i];

        fieldSize = 2 * (int)Math.Sqrt(totalArea) + 3;
    }

    public static int[] CreateSizes(int count1, int count2, int count3)
    {
        int[] sizes = new int[count1 + count2 + count3];
        for (int i = 0; i < sizes.Length; i++)
        {
            if (i < count3)
                sizes[i] = 3;
            else if (i < count3 + count2)
                sizes[i] = 2;
            else
                sizes[i] = 1;
        }
        return sizes;
    }

    public bool IsCorrect(Individual? individual)
    {
        if (individual == null || individual.X == null || individual.Y == null)
            return false;

        return individual.X.Length == sizes.Length && individual.Y.Length == sizes.Length;
    }

    public Individual CreateRandom()
    {
        Individual individual = new Individual(sizes.Length);
        for (int k = 0; k < sizes.Length; k++)
        {
            individual.X[k] = Random.Shared.Next(fieldSize);
            individual.Y[k] = Random.Shared.Next(fieldSize);
        }
        RemoveOverlaps(individual);
        individual.Area = CalculateArea(individual);
        return individual;
    }

    public Individual CreateChild(Individual[] parents, double mutationProbability)
    {
        Individual parent1 = parents[Random.Shared.Next(parents.Length)];
        Individual parent2 = parents[Random.Shared.Next(parents.Length)];

        Individual child = Crossover(parent1, parent2);

        for (int k = 0; k < sizes.Length; k++)
        {
            if (Random.Shared.NextDouble() < mutationProbability)
                Mutate(child, k);
        }

        Compact(child);
        child.Area = CalculateArea(child);
        return child;
    }

    public void GetBounds(Individual individual, out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = int.MaxValue;
        minY = int.MaxValue;
        maxX = int.MinValue;
        maxY = int.MinValue;

        for (int i = 0; i < sizes.Length; i++)
        {
            minX = Math.Min(minX, individual.X[i]);
            minY = Math.Min(minY, individual.Y[i]);
            maxX = Math.Max(maxX, individual.X[i] + sizes[i]);
            maxY = Math.Max(maxY, individual.Y[i] + sizes[i]);
        }
    }

    private int CalculateArea(Individual individual)
    {
        GetBounds(individual, out int minX, out int minY, out int maxX, out int maxY);
        return (maxX - minX) * (maxY - minY);
    }

    private Individual Crossover(Individual parent1, Individual parent2)
    {
        Individual child = new Individual(sizes.Length);
        for (int i = 0; i < sizes.Length; i++)
        {
            Individual parent = Random.Shared.Next(2) == 0 ? parent1 : parent2;
            child.X[i] = parent.X[i];
            child.Y[i] = parent.Y[i];
        }
        RemoveOverlaps(child);
        return child;
    }

    private void Mutate(Individual individual, int i)
    {
        GetBounds(individual, out int minX, out int minY, out int maxX, out int maxY);

        int oldX = individual.X[i];
        int oldY = individual.Y[i];

        individual.X[i] = Random.Shared.Next(minX, maxX - sizes[i] + 1);
        individual.Y[i] = Random.Shared.Next(minY, maxY - sizes[i] + 1);

        if (OverlapsAny(individual, i, sizes.Length))
        {
            individual.X[i] = oldX;
            individual.Y[i] = oldY;
        }
    }

    private void RemoveOverlaps(Individual individual)
    {
        for (int i = 0; i < sizes.Length; i++)
        {
            while (OverlapsAny(individual, i, i))
                individual.X[i]++;
        }
    }

    private void Compact(Individual individual)
    {
        GetBounds(individual, out int minX, out int minY, out _, out _);

        for (int i = 0; i < sizes.Length; i++)
        {
            while (individual.X[i] > minX)
            {
                individual.X[i]--;
                if (OverlapsAny(individual, i, sizes.Length))
                {
                    individual.X[i]++;
                    break;
                }
            }

            while (individual.Y[i] > minY)
            {
                individual.Y[i]--;
                if (OverlapsAny(individual, i, sizes.Length))
                {
                    individual.Y[i]++;
                    break;
                }
            }
        }
    }

    private bool OverlapsAny(Individual individual, int i, int count)
    {
        for (int j = 0; j < count; j++)
        {
            if (j != i && Overlaps(individual, i, j))
                return true;
        }
        return false;
    }

    private bool Overlaps(Individual individual, int i, int j)
    {
        return individual.X[i] < individual.X[j] + sizes[j]
            && individual.X[j] < individual.X[i] + sizes[i]
            && individual.Y[i] < individual.Y[j] + sizes[j]
            && individual.Y[j] < individual.Y[i] + sizes[i];
    }
}
