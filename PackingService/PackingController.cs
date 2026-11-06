using Microsoft.AspNetCore.Mvc;
using PackingLib;

namespace PackingService;

[ApiController]
public class PackingController : ControllerBase
{
    private const int MaxSize = 100;
    private const int MaxCount = 100000;

    [HttpPost("/random")]
    public ActionResult<Individual[]> CreateRandom(RandomRequest request)
    {
        string? error = CheckSizesAndCount(request.Sizes, request.Count);
        if (error != null)
            return BadRequest(error);

        GeneticOperations operations = new GeneticOperations(request.Sizes);

        Individual[] result = new Individual[request.Count];
        Parallel.For(0, request.Count, i => result[i] = operations.CreateRandom());
        return Ok(result);
    }

    [HttpPost("/children")]
    public ActionResult<Individual[]> CreateChildren(ChildrenRequest request)
    {
        string? error = CheckSizesAndCount(request.Sizes, request.Count);
        if (error != null)
            return BadRequest(error);

        if (request.MutationProbability < 0 || request.MutationProbability > 1)
            return BadRequest("Вероятность мутации должна быть от 0 до 1");

        if (request.Parents == null || request.Parents.Length == 0)
            return BadRequest("Не заданы родители");

        GeneticOperations operations = new GeneticOperations(request.Sizes);

        foreach (Individual parent in request.Parents)
        {
            if (!operations.IsCorrect(parent))
                return BadRequest("У родителя должно быть столько же координат, сколько квадратов");
        }

        Individual[] result = new Individual[request.Count];
        Parallel.For(0, request.Count, i => result[i] = operations.CreateChild(request.Parents, request.MutationProbability));
        return Ok(result);
    }

    private string? CheckSizesAndCount(int[] sizes, int count)
    {
        if (sizes == null || sizes.Length == 0)
            return "Не заданы размеры квадратов";

        foreach (int size in sizes)
        {
            if (size < 1 || size > MaxSize)
                return "Размер квадрата должен быть от 1 до " + MaxSize;
        }

        if (count < 1 || count > MaxCount)
            return "Количество особей должно быть от 1 до " + MaxCount;

        return null;
    }
}
