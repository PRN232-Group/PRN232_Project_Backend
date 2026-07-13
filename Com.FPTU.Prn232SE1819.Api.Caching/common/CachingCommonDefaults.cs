using System;
using System.Collections.Generic;
using System.Text;

namespace Com.FPTU.Prn232SE1819.Api.Caching.common;

public static partial  class CachingCommonDefaults
{
    public static int CacheTime = 30;//15 minus

    // fptu.ecommerce.{0}.id.{1} => fptu.ecommerce.product.id.1
    public static string CacheKey => "fptu.ecommerce.{0}.id.{1}";
    public static string AllCacheKey => "fptu.ecommerce.{0}.all";
    public static string CacheKeyHeader => "fptu.ecommerce.{0}.id";

    public static string CustomerByIdCacheKey => "fptu.ecommerce.customer.id.{0}";

}
