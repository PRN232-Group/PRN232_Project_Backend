using Com.FPTU.Prn232SE1819.Api.Caching.common;
using Microsoft.Extensions.DependencyInjection;

namespace Com.FPTU.Prn232SE1819.Api.Caching.extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddCacheServices(this IServiceCollection services)
    {
        services.AddScoped<IDataCached, DataCached>();
        return services;
    }
    public static string GetKey<T>(this IDataCached dataCached, T? entity,
       Func<T, dynamic> acquire) where T : class
        => string.Format(CachingCommonDefaults.CacheKey, typeof(T).Name, acquire(entity!));
}
