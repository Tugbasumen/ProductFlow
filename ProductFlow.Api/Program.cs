using Microsoft.EntityFrameworkCore;
using ProductFlow.Infrastructure.Persistence;
using ProductFlow.Api.Middleware;
using ProductFlow.Application.Interfaces;
using ProductFlow.Infrastructure.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProductFlowDbContext>(options =>
options.UseSqlServer(
builder.Configuration.GetConnectionString("ProductFlowDatabase")));
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
public partial class Program { }
