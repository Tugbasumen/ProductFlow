
using Microsoft.AspNetCore.Mvc;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Exceptions;
using ProductFlow.Application.Interfaces;

namespace ProductFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new
            {
                message = "Sayfa numarası en az 1 olmalıdır."
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Sayfa boyutu 1 ile 100 arasında olmalıdır."
            });
        }

        var result = await _productService.GetAllAsync(
            categoryId,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                product);
        }
        catch (DuplicateProductCodeException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _productService.UpdateAsync(
                id,
                request,
                cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/stock")]
    public async Task<IActionResult> AdjustStock(
        Guid id,
        AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _productService.AdjustStockAsync(
                id,
                request.QuantityChange,
                cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deactivated = await _productService.DeactivateAsync(
            id,
            cancellationToken);

        if (!deactivated)
            return NotFound();

        return NoContent();
    }
}
