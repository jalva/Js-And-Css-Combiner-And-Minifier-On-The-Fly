using System;
using System.Web;
using JsAndCssCombiner.LoggingService;
using JsAndCssCombiner.CacheService;

namespace JsAndCssCombiner
{
    public static class CombinerLiveSettings
    {
        #region Public Properties

        // These get initialized by the LiveSettingsService
        public static bool CombineJs { get; set; }
        public static bool CombineCss { get; set; }
        public static bool VersionOnly { get; set; }
        public static string JsVersion { get; set; }
        public static string CssVersion { get; set; }
        public static bool MinifyJs { get; set; }
        public static bool MinifyCss { get; set; }
        public static bool PrependCdnHostToImages { get; set; }

        private static readonly ILoggingService Logger;

        #endregion

        #region Constructor and Private Properties

        static CombinerLiveSettings()
        {
            Logger = new LoggingService.LoggingService();
            InitializeStaticMembers(typeof(CombinerLiveSettings), CombinerConstantsAndSettings.WebSettings.CombinerLiveSettingsFile);
        }

        static void InitializeStaticMembers(Type stronglyTypedSettingsObjType, string liveSettingsFileName)
        {

            try
            {
                if (HttpContext.Current != null)
                    liveSettingsFileName = HttpContext.Current.Server.MapPath(liveSettingsFileName);

                // Initialize properties who derive their values from a live settings file
                ICacheService cache = new CacheService.CacheService(Logger);
                var service = new LiveSettingsService.LiveSettingsService(cache); //ObjectFactory.GetInstance<ILiveSettingsService>();
                service.InitializeSettingsForFile(stronglyTypedSettingsObjType, liveSettingsFileName);
            }
            catch(Exception e)
            {
                Logger.Error("Combiner Live Settings File not found. Exception: " + e.Message);
            }
        }

        #endregion
    }
}
