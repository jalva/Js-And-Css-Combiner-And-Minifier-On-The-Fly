using System;
using System.IO;
using HtmlAgilityPack;
using JsAndCssCombiner.CombinerServices;
using JsAndCssCombiner.LoggingService;
using Persephone.Processing.Pipeline;

namespace JsAndCssCombiner.InterceptorFilterImplementation
{
    /// <summary>
    /// This class derives from Stream and can be used by both regular asp.net and asp.net mvc pages
    /// to replace the default HttpContext.Current.Response.Filter.
    /// It creates a pipeline of filters that will each execute sequentially. As each filter in the chain
    /// executes, it will modify the html by removing the script tags, removing the css tags, etc...
    /// They will parse the output html markup and replace all script and css tags with corresponding
    /// js and css tags (one each) to a url pointing to a handler that will handle the combining and 
    /// compressing of all replaced resources.
    /// </summary>
    public class CombinerResponseStream : ResponseFilterTemplate
    {
        private readonly bool _combineJs, _combineCss, _versionOnly, _minifyJs, _minifyCss, _prependCdnHostToImages;
        private readonly string _jsVersion, _cssVersion, _sharedVersion, _cdnHostToPrepend;
        private readonly int _ieVersion;
        private readonly string _requestedUrl, _combinedResourcesUrl;
        private readonly ICombinerService _combinerService;
        private readonly ILoggingService _logger;

        public CombinerResponseStream(
            Stream responseStream,
            bool combineJs,
            bool combineCss,
            bool minifyJs,
            bool minifyCss,
            bool versionOnly,
            bool prependCdnHostToImages,
            string jsVersion,
            string cssVersion,
            string sharedVersion,
            int ieVersion,
            string requestedUrl,
            ICombinerService combinerService,
            ILoggingService loggingService,
            string combinedResourcesUrl,
            string cdnHostToPrepend)
            : base(responseStream)
        {
            _combineJs = combineJs;
            _combineCss = combineCss;
            _minifyJs = minifyJs;
            _minifyCss = minifyCss;
            _versionOnly = versionOnly;
            _jsVersion = jsVersion;
            _cssVersion = cssVersion;
            _sharedVersion = sharedVersion;
            _ieVersion = ieVersion;
            _requestedUrl = requestedUrl;
            _combinerService = combinerService;
            _logger = loggingService;
            _combinedResourcesUrl = combinedResourcesUrl;
            _prependCdnHostToImages = prependCdnHostToImages;
            _cdnHostToPrepend = cdnHostToPrepend;
        }


        public override string ProcessHtml(string htmlToProcess)
        {
            // Do not process anything if all three flags are set to false
            if (!_combineJs && !_combineCss && !_versionOnly)
                return htmlToProcess;

            // Build the doc tree to be manipulated by the filter chain
            var doc = new HtmlDocument { OptionWriteEmptyNodes = true };
            doc.LoadHtml(htmlToProcess); 

            var filterContext = new CombinerFilterContext
                                    {
                                        Document = doc,
                                        CombineJs = _combineJs,
                                        CombineCss = _combineCss,
                                        MinifyJs = _minifyJs,
                                        MinifyCss = _minifyCss,
                                        VersionOnly = _versionOnly,
                                        JsVersion = _jsVersion,
                                        CssVersion = _cssVersion,
                                        SharedVersion = _sharedVersion,
                                        IeVersion = _ieVersion,
                                        RequestPath = _requestedUrl,
                                        CombinerService = _combinerService,
                                        CombinedResourcesUrl = _combinedResourcesUrl,
                                        PrependCdnHostToImages =  _prependCdnHostToImages,
                                        CdnHostToPrepend = _cdnHostToPrepend
                                    };

            try
            {
                // Build the filter chain and pass the doc tree to the individual filters.
                // Execute one filter at a time.
                // (Interceptor Filter implementation taken from: http://www.eggheadcafe.com/articles/20060609.asp)
                PipelineManager<CombinerFilterContext> pp =
                    PipelineFactory.CreateFromConfiguration<CombinerFilterContext>("JACombinerAndOptimizerGroup/combinerAndVersioningFilters");
                pp.ProcessFilter(ref filterContext);

                return filterContext.ParsedHtml;
            }
            catch(Exception e)
            {
                _logger.Error("Exception executing the js and css combiner filter chain for page: " + _requestedUrl + ". Exception: " + e.Message);
            }

            // Return the unmodified html if there was any problems executing the parsing filters!
            return htmlToProcess;
        }

    }

}