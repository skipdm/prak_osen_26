using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using PackingLib;
using PackingService;

namespace PackingServiceTests;

public class ServiceScenarioTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const int TotalArea = 10 * 1 + 6 * 4 + 4 * 9;

    private readonly HttpClient client;
    private readonly int[] sizes = GeneticOperations.CreateSizes(10, 6, 4);

    public ServiceScenarioTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Random_ReturnsRequestedNumberOfCorrectIndividuals()
    {
        Individual[] individuals = await RequestRandomAsync(50);

        Assert.Equal(50, individuals.Length);
        foreach (Individual individual in individuals)
            AssertCorrect(individual);
    }

    [Fact]
    public async Task Children_ReturnsRequestedNumberOfCorrectIndividuals()
    {
        Individual[] parents = await RequestRandomAsync(20);

        Individual[] children = await RequestChildrenAsync(parents, 35);

        Assert.Equal(35, children.Length);
        foreach (Individual child in children)
            AssertCorrect(child);
    }

    [Fact]
    public async Task FullSearch_BestAreaDecreases()
    {
        Individual[] population = await RequestRandomAsync(200);
        SortByArea(population);

        int firstArea = population[0].Area;
        int previousArea = firstArea;
        int half = population.Length / 2;

        for (int generation = 0; generation < 30; generation++)
        {
            Individual[] parents = new Individual[half];
            Array.Copy(population, parents, half);

            Individual[] children = await RequestChildrenAsync(parents, population.Length - half);
            Array.Copy(children, 0, population, half, children.Length);
            SortByArea(population);

            Assert.True(population[0].Area <= previousArea);
            previousArea = population[0].Area;
        }

        AssertCorrect(population[0]);
        Assert.True(population[0].Area < firstArea);
    }

    [Fact]
    public async Task Random_WithoutSizes_ReturnsBadRequest()
    {
        RandomRequest request = new RandomRequest { Sizes = new int[0], Count = 10 };

        HttpResponseMessage response = await client.PostAsJsonAsync("/random", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Random_WithZeroCount_ReturnsBadRequest()
    {
        RandomRequest request = new RandomRequest { Sizes = sizes, Count = 0 };

        HttpResponseMessage response = await client.PostAsJsonAsync("/random", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Random_WithNegativeSize_ReturnsBadRequest()
    {
        RandomRequest request = new RandomRequest { Sizes = new int[] { 2, -1, 3 }, Count = 10 };

        HttpResponseMessage response = await client.PostAsJsonAsync("/random", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Random_WithBrokenJson_ReturnsBadRequest()
    {
        StringContent content = new StringContent("это не JSON", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/random", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Children_WithoutParents_ReturnsBadRequest()
    {
        ChildrenRequest request = new ChildrenRequest
        {
            Sizes = sizes,
            Parents = new Individual[0],
            MutationProbability = 0.1,
            Count = 10
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/children", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Children_WithWrongParent_ReturnsBadRequest()
    {
        Individual wrongParent = new Individual(3);
        ChildrenRequest request = new ChildrenRequest
        {
            Sizes = sizes,
            Parents = new Individual[] { wrongParent },
            MutationProbability = 0.1,
            Count = 10
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/children", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Children_WithWrongMutationProbability_ReturnsBadRequest()
    {
        Individual[] parents = await RequestRandomAsync(10);
        ChildrenRequest request = new ChildrenRequest
        {
            Sizes = sizes,
            Parents = parents,
            MutationProbability = 1.5,
            Count = 10
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/children", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Individual[]> RequestRandomAsync(int count)
    {
        RandomRequest request = new RandomRequest { Sizes = sizes, Count = count };

        HttpResponseMessage response = await client.PostAsJsonAsync("/random", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Individual[]? individuals = await response.Content.ReadFromJsonAsync<Individual[]>();
        Assert.NotNull(individuals);
        return individuals;
    }

    private async Task<Individual[]> RequestChildrenAsync(Individual[] parents, int count)
    {
        ChildrenRequest request = new ChildrenRequest
        {
            Sizes = sizes,
            Parents = parents,
            MutationProbability = 0.1,
            Count = count
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/children", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Individual[]? individuals = await response.Content.ReadFromJsonAsync<Individual[]>();
        Assert.NotNull(individuals);
        return individuals;
    }

    private void SortByArea(Individual[] population)
    {
        Array.Sort(population, (a, b) => a.Area.CompareTo(b.Area));
    }

    private void AssertCorrect(Individual individual)
    {
        Assert.Equal(sizes.Length, individual.X.Length);
        Assert.Equal(sizes.Length, individual.Y.Length);

        for (int i = 0; i < sizes.Length; i++)
        {
            for (int j = i + 1; j < sizes.Length; j++)
            {
                bool overlaps = individual.X[i] < individual.X[j] + sizes[j]
                    && individual.X[j] < individual.X[i] + sizes[i]
                    && individual.Y[i] < individual.Y[j] + sizes[j]
                    && individual.Y[j] < individual.Y[i] + sizes[i];
                Assert.False(overlaps);
            }
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < sizes.Length; i++)
        {
            minX = Math.Min(minX, individual.X[i]);
            minY = Math.Min(minY, individual.Y[i]);
            maxX = Math.Max(maxX, individual.X[i] + sizes[i]);
            maxY = Math.Max(maxY, individual.Y[i] + sizes[i]);
        }

        Assert.Equal((maxX - minX) * (maxY - minY), individual.Area);
        Assert.True(individual.Area >= TotalArea);
    }
}
