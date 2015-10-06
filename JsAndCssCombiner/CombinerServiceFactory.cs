using JsAndCssCombiner.CombinerServices;

namespace JsAndCssCombiner
{
    public class CombinerServiceFactory
    {
        /// <summary>
        /// Abstracts the creation of an instance of ICombinerService
        /// </summary>
        /// <returns></returns>
        public static ICombinerService CreateCombinerService()
        {
            var logger = new LoggingService.LoggingService();
            var cacheService = new CacheService.CacheService(logger);
            var minifier = new ResourceMinifier();
            var myCombiner = new LongUrlCombinerService(cacheService, minifier, logger); //ObjectFactory.GetInstance<ICombinerService>();

            return myCombiner;
        }
    }
}
