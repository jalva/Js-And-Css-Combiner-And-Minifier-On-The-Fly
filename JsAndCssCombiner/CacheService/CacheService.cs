using System;
using System.Runtime.Caching;
using System.Threading;
using JsAndCssCombiner.LoggingService;

namespace JsAndCssCombiner.CacheService
{ 
    public class CacheService : ICacheService
    {
        private readonly ILoggingService _logger;
        private readonly MemoryCache _cache;

        public CacheService(ILoggingService logger)
        {
            _logger = logger;
            _cache = MemoryCache.Default;
        }


        #region ICacheService Members

        public T Get<T>(AutoResetEvent resetEvent, string cacheKey, Func<T> getItemCallback, Func<CacheItemPolicy> getExpiration) where T : class
       {
           T item = _cache.Get(cacheKey) as T;
           if (item == null)
           {
               resetEvent.WaitOne();
               item = _cache.Get(cacheKey) as T;
               if (item == null)
                   try
                   {
                       item = getItemCallback();
                       if (item != null)
                           _cache.Add(cacheKey, item, getExpiration());
                   }
                   catch (Exception ex)
                   {
                       _logger.Error("Error retrieving from cache. cacheKey=" + cacheKey + "; type=" + typeof(T) + "; " + ex.Message);
                   }
               resetEvent.Set();
           }
           return item;
       }


       #endregion
    }



}
