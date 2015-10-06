using System.Linq;
using HtmlAgilityPack;

namespace JsAndCssCombiner.InterceptorFilterImplementation.Filters
{
    public class JsVersoningFilter : BaseFilter
    {
        protected override void Process2(ref CombinerFilterContext data)
        {
            // This filter will execute only if the css haven't been combined,
            // since the combiner filter does the versioning as well. 
            // This is done for performance reasons to avoid having to traverse the 
            // document tree again.
            if (!data.CombineJs && data.VersionOnly)
            {
                string versionQueryString = data.CombinerService.GetVersionQueryString(data.JsVersion,
                                                                                       data.SharedVersion);

                if (ScriptIncludeNodes == null)
                    return;

                var scriptNodes = ScriptIncludeNodes.ToList();
                foreach (HtmlNode script in scriptNodes)
                {
                    string src = script.Attributes["src"].Value;
                    string querySeparator = (src.IndexOf('?') > 0 ? "&" : "?");

                    script.Attributes["src"].Value += querySeparator + versionQueryString;
                }
            }
        }
    }
}
