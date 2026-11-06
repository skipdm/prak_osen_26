namespace PackingLib;

public class Individual
{
    public int[] X { get; set; }
    public int[] Y { get; set; }

    public int Area { get; set; }

    public Individual()
    {
        X = new int[0];
        Y = new int[0];
    }

    public Individual(int count)
    {
        X = new int[count];
        Y = new int[count];
    }
}
