using Com.FPTU.Prn232SE1819.Api.Caching.extensions;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
using Com.FPTU.Prn232SE1919.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


/*<Dongbh Add: enable all services*/
builder.Services.AddMemoryCache();
builder.Services.EcommerceInfrastructureDatabase(builder.Configuration);
builder.Services.AddCacheServices();
builder.Services.AddDataServices();
/*</Dongbh Add*/

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
