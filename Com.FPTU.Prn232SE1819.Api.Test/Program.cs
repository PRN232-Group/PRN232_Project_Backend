
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
using Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Repositories;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Common;


//1. lay ra 1 mang cac products => test xem repository chay the nao?
DbFactoryContext dbContext =
    new DbFactoryContext(() => new ProductDbContext());


//2. init ApplicationDbContext
IApplicationDbContext db = new ApplicationDbContext(dbContext);
//3. Khoi tao Repository
IRepository<Product> productRepository = new Repository<Product>(db);

//4. Show data
var data = productRepository.Find(1);

Console.WriteLine($"Product ID: {data.ProductId}, Description: {data.Description} and Product Name: {data.Name}");
