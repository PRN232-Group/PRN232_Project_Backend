using System;
using System.Collections.Generic;
using System.Text;

namespace Com.FPTU.Prn232SE1819.Api.Caching;


public interface IDataCached
{
    T Get<T>(string key, Func<T> acquire, int? cacheTime = null);
    T Get<T>(string key);
    object Get(string key);
    IList<string> GetKeys();
    IList<T> GetValues<T>(string pattern);

    void Set<T>(string key, T? value, int cacheTime);
    bool IsSet(string key);
    
    void Remove(string key);
    void Clear();
}
