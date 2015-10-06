using HtmlAgilityPack;
using JsAndCssCombiner.CombinerServices;

namespace JsAndCssCombiner.InterceptorFilterImplementation
{
    public class CombinerFilterContext
    {
        public HtmlDocument Document { get; set; }
        public bool CombineJs { get; set; }
        public bool CombineCss { get; set; }
        public bool MinifyJs { get; set; }
        public bool MinifyCss { get; set; }
        public bool VersionOnly { get; set; }
        public string JsVersion { get; set; }
        public string CssVersion { get; set; }
        public string SharedVersion { get; set; }
        public int IeVersion { get; set; }
        public string RequestPath { get; set; }
        public string CombinedResourcesUrl { get; set; }
        public ICombinerService CombinerService { get; set; }
        public bool RewriteImagePaths { get; set; }
        public bool PrependCdnHostToImages { get; set; }
        public string CdnHostToPrepend { get; set; }

        public string ParsedHtml { get { return Document.DocumentNode.WriteContentTo(); } }
    }
}
