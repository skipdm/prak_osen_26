namespace PackingLib;

public class RandomRequest
{
    public int[] Sizes { get; set; } = new int[0];
    public int Count { get; set; }
}

public class ChildrenRequest
{
    public int[] Sizes { get; set; } = new int[0];
    public Individual[] Parents { get; set; } = new Individual[0];
    public double MutationProbability { get; set; }
    public int Count { get; set; }
}
