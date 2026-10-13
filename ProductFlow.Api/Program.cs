using Microsoft.EntityFrameworkCore;
using ProductFlow.Infrastructure.Persistence;
using ProductFlow.Api.Middleware;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProductFlowDbContext>(options =>
options.UseSqlServer(
builder.Configuration.GetConnectionString("ProductFlowDatabase")));

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
