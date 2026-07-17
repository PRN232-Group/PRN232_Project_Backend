using Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Common;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<InteriorStudioDbContext>()
    .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=InteriorStudio;User Id=sa;Password=1234567890;TrustServerCertificate=True")
    .Options;

var dbFactory = new DbFactoryContext(() => new InteriorStudioDbContext(options));
IApplicationDbContext db = new ApplicationDbContext(dbFactory);
IRepository<Role> roleRepo = new Repository<Role>(db);

var role = roleRepo.Find(1);
if (role != null)
    Console.WriteLine($"Role ID: {role.Id}, Name: {role.Name}");
else
    Console.WriteLine("No role with Id=1. Seed Roles first.");
