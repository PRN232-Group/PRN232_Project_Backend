
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Repositories;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Common;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;
using Com.FPTU.Prn232SE1919.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Com.FPTU.Prn232SE1919.Services.Extensions;

public static class EcommerceContextServiceExtensions
{
    //1. Ta ra 1 dich vu de thuc hien ket noi database
    public static IServiceCollection EcommerceInfrastructureDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ProductDbContext>(options =>
        {
            options.UseSqlServer(config.GetConnectionString("FptEcommerceDbConn"),
                sqlOptions => sqlOptions.CommandTimeout(60));

            // options.UseLazyLoadingProxies();//must be install the pakcage Micrsoft.EntityFrameworkCore.Proxies
        });

        //2. Add dbcontext by service --> kích hoạt cơ chế DI để inject ProductDbContext vào các service khác
        // khởi tạo tất cả các đối tượng mà có thể dùng chung bằng cách nhét vào DI container
        services.AddScoped<Func<ProductDbContext>>(
                provider => () => provider.GetService<ProductDbContext>()
            );

    
        services.AddScoped<DbFactoryContext>();//giong nhu new DbFactoryContext
  
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();


        return services;

    }
    //2. data services
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        //== co IService khac ma muon add-on thi anh em add vao day
        return services;
    }
}
