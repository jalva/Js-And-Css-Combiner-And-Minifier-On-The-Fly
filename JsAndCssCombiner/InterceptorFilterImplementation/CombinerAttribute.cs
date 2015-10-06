using System;
using System.Web;
using System.Web.Mvc;
using JsAndCssCombiner.CombinerServices;

namespace JsAndCssCombiner.InterceptorFilterImplementation
{
    /// <summary>
    /// MVC Attribute to be used on Action Methods that need to have their JavaScript and Css
    /// combined and minimized;
    /// </summary>
    public class CombinerAttribute : ActionFilterAttribute
    {
        private readonly bool _applyOutputCaching, _combineJs, _combineCss, _versionOnly, _minifyJs, _minifyCss, _prependCdnHostToImages;

        /// <summary>
        /// This constructor is used when all settings are defalted to True.
        /// </summary>
        public CombinerAttribute()
        {
            _applyOutputCaching = true;
            _combineJs = true;
            _combineCss = true;
            _versionOnly = true;
            _minifyJs = true;
            _minifyCss = true;
            _prependCdnHostToImages = true;
        }

        /// <summary>
        /// This constructor is used when we want to combine and minify the resources but want to control the outputcaching
        /// </summary>
        /// <param name="applyOutputCaching"></param>
        public CombinerAttribute(bool applyOutputCaching):this()
        {
            _applyOutputCaching = applyOutputCaching;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applyOutputCaching">Flag used to apply output cache to the page</param>
        /// <param name="combineJs">Flag used to combine javascript</param>
        /// <param name="combineCss">Flag used to combine css</param>
        /// <param name="versionOnly">Used only when either combineJs and/or combineCss is false. A version is always appended when those are true.</param>
        /// <param name="minifyJs">This will get ignored if 'combineJs' equals false</param>
        /// <param name="minifyCss">This will get ignored if 'combineCss' equals false</param>
        /// <param name="prependCdnHostToImages">Flag used to prepend a cdn host to all images on the page</param>
        public CombinerAttribute(bool applyOutputCaching, bool combineJs, bool combineCss, bool versionOnly, bool minifyJs, bool minifyCss, bool prependCdnHostToImages)
        {
            _applyOutputCaching = applyOutputCaching;
            _combineJs = combineJs;
            _combineCss = combineCss;
            _versionOnly = versionOnly;
            _minifyJs = minifyJs;
            _minifyCss = minifyCss;
            _prependCdnHostToImages = prependCdnHostToImages;
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {

            bool isIe = HttpContext.Current.Request.Browser.Browser.Trim()
                .Equals("IE", StringComparison.InvariantCultureIgnoreCase);
            int ieVersion = isIe ? HttpContext.Current.Request.Browser.MajorVersion : 0;

            ICombinerService myCombiner = CombinerServiceFactory.CreateCombinerService();

            var logger = new LoggingService.LoggingService();

            // Set the Response.Filter to our custom Stream class that will do the parsing of the html 
            // and the replacement of the script and css tags with the combined resources tags.
            filterContext.HttpContext.Response.Filter =
                new CombinerResponseStream(
                    filterContext.HttpContext.Response.Filter,
                    CombinerLiveSettings.CombineJs && _combineJs,
                    CombinerLiveSettings.CombineCss && _combineCss,
                    CombinerLiveSettings.MinifyJs && _minifyJs,
                    CombinerLiveSettings.MinifyCss && _minifyCss,
                    CombinerLiveSettings.VersionOnly && _versionOnly,
                    CombinerLiveSettings.PrependCdnHostToImages && _prependCdnHostToImages,
                    CombinerLiveSettings.JsVersion,
                    CombinerLiveSettings.CssVersion,
                    CombinerConstantsAndSettings.JsAndCssSharedVersion,
                    ieVersion,
                    HttpContext.Current.Request.Url.AbsolutePath.TrimEnd('/'),
                    myCombiner,
                    logger,
                    CombinerConstantsAndSettings.WebSettings.ComboScriptUrl,
                    CombinerConstantsAndSettings.WebSettings.ImagesCdnHostToPrepend
                    );
            
            if (_applyOutputCaching)
            {
                // Set cacheability
                filterContext.HttpContext.Response.Cache.SetExpires(DateTime.Now.AddMonths(1));
                filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.Public);
                filterContext.HttpContext.Response.Cache.SetValidUntilExpires(true);
                filterContext.HttpContext.Response.Cache.VaryByParams["*"] = true;

                // Set file dependency for output cache
                // when the live.js is updated we need to invalidate the OutputCache for this page
                string path = CombinerConstantsAndSettings.WebSettings.CombinerLiveSettingsFile; 
                string liveSettingsFilePath = filterContext.HttpContext.Server.MapPath(path);

                filterContext.HttpContext.Response.AddFileDependency(liveSettingsFilePath);
            }
            
            base.OnResultExecuting(filterContext);
        }

    }
}