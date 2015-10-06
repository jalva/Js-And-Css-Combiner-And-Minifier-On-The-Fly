using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace JsAndCssCombiner.CombinerServices
{
    /// <summary>
    /// Generates the combined script and css tags that replace the existing individual tags.
    /// Serves the combined content to those combined script and css tags.
    /// </summary>
    public interface ICombinerService
    {
        string[] GetCombinedScriptUrls(string pageUrl, IList<string> scriptUrls, string manualVersion, string sharedVersion, string combinedHandlerUrl, bool minify, bool rewriteImagePaths);
        string[] GetCombinedCssUrls(string pageUrl, IList<string> cssUrls, string manualVersion, string sharedVersion, string combinedHandlerUrl, bool minify, bool rewriteImagePaths);
        string GetVersionQueryString(string manualVersion, string sharedVersion);

        byte[] ServeCombinedContent(int ieVersion, NameValueCollection queryStringParms, Func<string, string> pathMapper, Func<string, string> fileReader, string imagesHostToPrepend);

        string MinifyJs(string js);
        string MinifyCss(string css);
    }
}
