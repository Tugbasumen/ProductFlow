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
            nameof(GetAll),
            new { id = product.Id },
            product);
    }


}
