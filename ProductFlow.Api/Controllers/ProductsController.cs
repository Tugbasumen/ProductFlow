using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductFlowDbContext _dbContext;


    public ProductsController(ProductFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(products);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        return Ok(product);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        product.UpdateDetails(
            request.Name,
            request.Description,
            request.Price);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductRequest request)
    {
        var product = new Product(
            request.Code,
            request.Name,
            request.Price,
            request.StockQuantity,
            request.Description);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
    nameof(GetById),
    new { id = product.Id },
    product);
    }
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        product.Deactivate();

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

}
