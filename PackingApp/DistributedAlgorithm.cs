using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PackingLib;

namespace PackingApp;

public class NoServicesException : Exception
{
    public NoServicesException() : base("Нет доступных вычислительных сервисов")
    {
    }
}

public class DistributedAlgorithm
{
    private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    private readonly GeneticOperations operations;
    private readonly int populationSize;
    private readonly double mutationProbability;
    private readonly List<ServiceInfo> services;
    private Individual[] population = new Individual[0];

    public int[] Sizes => operations.Sizes;
    public int Generation { get; private set; }
    public Individual Best => population[0];

    public DistributedAlgorithm(int count1, int count2, int count3, int populationSize, double mutationProbability, List<ServiceInfo> services)
    {
        this.populationSize = populationSize;
        this.mutationProbability = mutationProbability;
        this.services = services;

        operations = new GeneticOperations(GeneticOperations.CreateSizes(count1, count2, count3));
    }

    public async Task CreatePopulationAsync()
    {
        List<Individual> individuals = await RequestIndividualsAsync(null, populationSize);

        population = individuals.ToArray();
        SortPopulation();
    }

    public async Task NextGenerationAsync()
    {
        int half = population.Length / 2;

        Individual[] parents = new Individual[half];
        Array.Copy(population, parents, half);

        List<Individual> children = await RequestIndividualsAsync(parents, population.Length - half);
        for (int i = 0; i < children.Count; i++)
            population[half + i] = children[i];

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

    private async Task<List<Individual>> RequestIndividualsAsync(Individual[]? parents, int needed)
    {
        List<Individual> result = new List<Individual>();

        while (result.Count < needed)
        {
            List<ServiceInfo> working = new List<ServiceInfo>();
            foreach (ServiceInfo service in services)
            {
                if (service.IsWorking)
                    working.Add(service);
            }

            if (working.Count == 0)
                throw new NoServicesException();

            int remaining = needed - result.Count;
            List<Task<Individual[]>> tasks = new List<Task<Individual[]>>();

            for (int i = 0; i < working.Count; i++)
            {
                int count = remaining / working.Count;
                if (i < remaining % working.Count)
                    count++;

                if (count == 0)
                    continue;

                if (parents == null)
                    tasks.Add(RequestRandomAsync(working[i], count));
                else
                    tasks.Add(RequestChildrenAsync(working[i], parents, count));
            }

            Individual[][] answers = await Task.WhenAll(tasks);
            foreach (Individual[] answer in answers)
                result.AddRange(answer);
        }

        return result;
    }

    private Task<Individual[]> RequestRandomAsync(ServiceInfo service, int count)
    {
        RandomRequest request = new RandomRequest { Sizes = Sizes, Count = count };
        return PostAsync(service, "/random", JsonContent.Create(request), count);
    }

    private Task<Individual[]> RequestChildrenAsync(ServiceInfo service, Individual[] parents, int count)
    {
        ChildrenRequest request = new ChildrenRequest
        {
            Sizes = Sizes,
            Parents = parents,
            MutationProbability = mutationProbability,
            Count = count
        };
        return PostAsync(service, "/children", JsonContent.Create(request), count);
    }

    private async Task<Individual[]> PostAsync(ServiceInfo service, string path, HttpContent content, int count)
    {
        try
        {
            HttpResponseMessage response = await client.PostAsync("http://" + service.Address + path, content);

            if (!response.IsSuccessStatusCode)
            {
                service.Error = "сервис вернул ошибку " + (int)response.StatusCode;
            }
            else
            {
                Individual[]? individuals = await response.Content.ReadFromJsonAsync<Individual[]>();

                if (individuals != null && IsCorrectAnswer(individuals, count))
                    return individuals;

                service.Error = "сервис прислал неверный ответ";
            }
        }
        catch (HttpRequestException e)
        {
            service.Error = "нет связи (" + e.Message + ")";
        }
        catch (TaskCanceledException)
        {
            service.Error = "сервис не ответил вовремя";
        }
        catch (JsonException)
        {
            service.Error = "сервис прислал непонятный ответ";
        }

        service.IsWorking = false;
        return new Individual[0];
    }

    private bool IsCorrectAnswer(Individual[] individuals, int count)
    {
        if (individuals.Length != count)
            return false;

        foreach (Individual individual in individuals)
        {
            if (!operations.IsCorrect(individual))
                return false;
        }

        return true;
    }
}
