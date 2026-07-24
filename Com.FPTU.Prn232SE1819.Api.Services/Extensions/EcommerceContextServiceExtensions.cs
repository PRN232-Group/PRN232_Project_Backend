using Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Common;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Interceptors;
using Com.FPTU.Prn232SE1819.Api.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Com.FPTU.Prn232SE1819.Api.Services.Extensions;

public static class EcommerceContextServiceExtensions
{
    public static IServiceCollection EcommerceInfrastructureDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<InteriorStudioDbContext>((sp, options) =>
        {
            options.UseSqlServer(config.GetConnectionString("InteriorStudioDbConn"),
                sqlOptions => sqlOptions.CommandTimeout(60));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<Func<InteriorStudioDbContext>>(
            provider => () => provider.GetService<InteriorStudioDbContext>()!);

        services.AddScoped<DbFactoryContext>();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInteriorDesignService, InteriorDesignService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IQuotationRequestService, QuotationRequestService>();
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        services.AddScoped<SystemLogService>();
        services.AddScoped<ISystemLogService>(sp => sp.GetRequiredService<SystemLogService>());
        services.AddScoped<IAuditService>(sp => sp.GetRequiredService<SystemLogService>());

        services.AddScoped<IDesignRequestService, DesignRequestService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
