using Com.FPTU.Prn232SE1819.Api.Caching.common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
namespace Com.FPTU.Prn232SE1819.Api.Caching;

public class DataCached : IDataCached
{

    private readonly IMemoryCache _memoryCache; // Tái sử dụng của microsoft

    private static readonly ConcurrentDictionary<string, bool> _allKeys;
    public CancellationTokenSource _cancellationTokenSource;

    static DataCached()
    {
        _allKeys = new ConcurrentDictionary<string, bool>();
    }
    public DataCached(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _cancellationTokenSource = new CancellationTokenSource();

    }


    public void Clear()
    {
        throw new NotImplementedException();
    }

    public T Get<T>(string key, Func<T> acquire, int? cacheTime = null)
    {

        //item already is in cached, so return it
        if (_memoryCache.TryGetValue(key, out T? value))
            return value!;

        //or create it using passed function
        var result = acquire();

        //and set in cached (if cached time is defined)
        if ((cacheTime ?? CachingCommonDefaults.CacheTime) > 0)
        {
            Set(key, result, cacheTime ?? CachingCommonDefaults.CacheTime);
        }
        return result;
    }

    public T Get<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
            return value!;

        return default(T)!; //from c# 9.0
    }

    public object Get(string key)
    {
        if (_memoryCache.TryGetValue(key, out object? value))
            return value!;
        return null!;
    }

    public IList<string> GetKeys()
    {
        throw new NotImplementedException();
    }

    public IList<T> GetValues<T>(string pattern)
    {
        /*Collect all data from the memcached*/
        IList<T> values = new List<T>();
        //get cache keys that matches pattern
        var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var matchesKeys = _allKeys
                            .Where(p => p.Value)
                            .Select(p => p.Key)
                            .Where(key => regex.IsMatch(key)).ToList();
        //loop to get value
        foreach (var key in matchesKeys)
        {
            _memoryCache.TryGetValue(key, out T? value);
            if (value != null)
                values.Add(value);
        }

        return values;
    }

    public bool IsSet(string key)
    {
        return _allKeys.ContainsKey(key);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(_removeKey(key));//vua remove trong dictionary va remove data luon
    }

    public void Set<T>(string key, T? value, int cacheTime)
    {
        if (value != null)
        {
            _memoryCache.Set(_addKey(key), value,
                _getMemoryCacheEntryOptions(TimeSpan.FromMinutes(cacheTime)));
        }
    }

    private MemoryCacheEntryOptions _getMemoryCacheEntryOptions(TimeSpan cacheTime)
    {
        var options = new MemoryCacheEntryOptions()
            .AddExpirationToken(new CancellationChangeToken(_cancellationTokenSource.Token))
            .SetSize(0);

        options.AbsoluteExpirationRelativeToNow = cacheTime;
        return options;

    }
    private string _addKey(string key)
    {
        _allKeys.TryAdd(key, true);
        return key;
    }
    private string _removeKey(string key)
    {
        _tryRemoveKey(key);
        return key;
    }
    private void _tryRemoveKey(string key)
    {
        //try to remove key from the dictionary
        if (!_allKeys.TryRemove(key, out _))
        {
            _allKeys.TryUpdate(key, false, true);
        }
    }
}
