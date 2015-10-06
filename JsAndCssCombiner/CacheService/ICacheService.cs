using System;
using System.Runtime.Caching;
using System.Threading;

namespace JsAndCssCombiner.CacheService
{
    public interface ICacheService
    {
        T Get<T>(AutoResetEvent resetEvent, string cacheKey, Func<T> getItemCallback, Func<CacheItemPolicy> getExpiration) where T : class;
    }
}
