using Microsoft.AspNetCore.Mvc;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Interfaces;

namespace ProductFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StockMovementsController : ControllerBase
{
    private readonly IStockMovementService _stockMovementService;

    public StockMovementsController(
        IStockMovementService stockMovementService)
    {
        _stockMovementService = stockMovementService;
    }

    [HttpPost]
    public async Task<ActionResult<StockMovementResponse>> Create(
        [FromBody] CreateStockMovementRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockMovementService.CreateAsync(
                request, cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "Ürün bulunamadı."
                });
            }

            return CreatedAtAction(
                nameof(GetAll),
                new { productId = result.ProductId },
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockMovementResponse>>> GetAll(
        [FromQuery] Guid? productId,
        CancellationToken cancellationToken)
    {
        var movements = await _stockMovementService.GetAllAsync(
            productId, cancellationToken);

        return Ok(movements);
    }
}