using System;
using System.Web;
using System.Web.UI.WebControls;
using JsAndCssCombiner.CombinerServices;

namespace JsAndCssCombiner.InterceptorFilterImplementation
{
    /// <summary>
    /// Asp.net server control to be used on pages that need to have their JavaScript and Css
    /// combined and minimized;
    /// </summary>
    public class CombinerWebControl : WebControl
    {
        /// <summary>
        /// Default is true
        /// </summary>
        public bool? ApplyOutputCaching { get; set; }
        /// <summary>
        /// Default is true
        /// </summary>
        public bool? CombineJs { get; set; }
        /// <summary>
        /// Default is true
        /// </summary>
        public bool? CombineCss { get; set; }
        /// <summary>
        /// Default is true.
        /// Used only when either CombineJs and/or CombineCss is false.
        /// A version is always appended when combining resources.
        /// </summary>
        public bool? VersionOnly { get; set; }
        /// <summary>
        /// Default is true. 
        /// Gets ignored if 'CombineJs' is false.
        /// </summary>
        public bool? MinifyJs { get; set; }
        /// <summary>
        /// Default is true.
        /// Gets ignored if 'CombineCss' is false.
        /// </summary>
        public bool? MinifyCss { get; set; }

        /// <summary>
        /// Default is true
        /// </summary>
        public bool? PrependCdnHostToImages { get; set; }

        protected override void OnPreRender(EventArgs e)
        {

            bool isIe = HttpContext.Current.Request.Browser.Browser.Trim()
                .Equals("IE", StringComparison.InvariantCultureIgnoreCase);
            int ieVersion = isIe ? HttpContext.Current.Request.Browser.MajorVersion : 0;

            ICombinerService myCombiner = CombinerServiceFactory.CreateCombinerService();
            
            var logger = new LoggingService.LoggingService();

            HttpContext.Current.Response.Filter =
                new CombinerResponseStream(
                    HttpContext.Current.Response.Filter,
                    CombinerLiveSettings.CombineJs && CombineJs.GetValueOrDefault(true),
                    CombinerLiveSettings.CombineCss && CombineCss.GetValueOrDefault(true),
                    CombinerLiveSettings.MinifyJs && MinifyJs.GetValueOrDefault(true),
                    CombinerLiveSettings.MinifyCss && MinifyCss.GetValueOrDefault(true),
                    CombinerLiveSettings.VersionOnly && VersionOnly.GetValueOrDefault(true),
                    CombinerLiveSettings.PrependCdnHostToImages && PrependCdnHostToImages.GetValueOrDefault(true),
                    CombinerLiveSettings.JsVersion,
                    CombinerLiveSettings.CssVersion,
                    CombinerConstantsAndSettings.JsAndCssSharedVersion,
                    ieVersion,
                    HttpContext.Current.Request.Url.AbsolutePath,
                    myCombiner,
                    logger,
                    CombinerConstantsAndSettings.WebSettings.ComboScriptUrl,
                    CombinerConstantsAndSettings.WebSettings.ImagesCdnHostToPrepend
                    );

            if (ApplyOutputCaching.GetValueOrDefault(true))
            {
                // Set cacheability
                HttpContext.Current.Response.Cache.SetExpires(DateTime.Now.AddMonths(1));
                HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.Public);
                HttpContext.Current.Response.Cache.SetValidUntilExpires(true);
                HttpContext.Current.Response.Cache.VaryByParams["*"] = true;

                // Set file dependency for output cache
                // when the live.js is updated we need to invalidate the OutputCache for this page
                string path = CombinerConstantsAndSettings.WebSettings.CombinerLiveSettingsFile;
                string liveSettingsFilePath = HttpContext.Current.Server.MapPath(path);

                HttpContext.Current.Response.AddFileDependency(liveSettingsFilePath);
            }
        }
    }
}
