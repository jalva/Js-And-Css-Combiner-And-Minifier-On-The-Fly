using System.Linq;
using HtmlAgilityPack;

namespace JsAndCssCombiner.InterceptorFilterImplementation.Filters
{
    public class CssVersioningFilter : BaseFilter
    {
        protected override void Process2(ref CombinerFilterContext data)
        {
            // This filter will execute only if the scripts haven't been combined,
            // since the combiner filter does the versioning as well. 
            // This is done for performance reasons to avoid having to traverse the 
            // document tree again.
            if (!data.CombineCss && data.VersionOnly)
            {
                string versionQueryString = data.CombinerService.GetVersionQueryString(data.JsVersion,
                                                                                       data.SharedVersion);
                if (CssNodes == null)
                    return;

                var cssNodes = CssNodes.ToList();
                foreach (HtmlNode css in cssNodes)
                {
                    string src = css.Attributes["href"].Value;
                    string querySeparator = (src.IndexOf('?') > 0 ? "&" : "?");

                    css.Attributes["href"].Value += querySeparator + versionQueryString;
                }
            }
        }
    }
}
