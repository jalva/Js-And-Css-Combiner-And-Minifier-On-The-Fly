using System.Text;
using HtmlAgilityPack;
using System.Linq;

namespace JsAndCssCombiner.InterceptorFilterImplementation.Filters
{
    public class JsCombinerFilter : BaseFilter
    {

        /// <summary>
        /// Replaces all local script include tags on the page with a tag pointing to the combined resources
        /// of the tags removed. 
        /// Appends the combined resources tag to the end of the document.
        /// Moves all inline scripts to the end of the document.
        /// Versions the resulting combined script tag (for performance reasons we're combining this operation in this filter).
        /// </summary>
        protected override void Process2(ref CombinerFilterContext data)
        {
            if (!data.CombineJs)
                return;

            if (ScriptIncludeNodes == null)
                return;

            var scriptNodes = ScriptIncludeNodes.ToList();

            // Get the url pointing to the combined js of the tags removed
            // We're sending only distinct urls in order to eliminate duplicate loads of the same file...
            string[] combinedScriptsUrls =
                data.CombinerService.GetCombinedScriptUrls(
                    data.RequestPath,
                    scriptNodes.Select(n => n.Attributes["src"].Value).Distinct().ToList(),
                    data.JsVersion,
                    data.SharedVersion,
                    data.CombinedResourcesUrl,
                    data.MinifyJs,
                    data.PrependCdnHostToImages);
            
            // Remove all script include tags and insert the new combined script tag
            foreach (HtmlNode script in scriptNodes)
            {
                // If we encounter an include script tag that also has some script in its body
                // then we have to create a new script tag with that content and replace the original one with this new one
                if (!string.IsNullOrEmpty(script.InnerHtml))
                {
                    var newScript =
                        HtmlNode.CreateNode("<script type='text/javascript'>" + script.InnerHtml + "</script>");

                    script.ParentNode.ReplaceChild(newScript, script);
                    continue;
                }

                script.ParentNode.RemoveChild(script, false);
            }


            var body = Doc.DocumentNode.SelectSingleNode(@"//body");
            var sb = new StringBuilder();

            foreach (string combinedUrl in combinedScriptsUrls)
            {
                sb.Append("<script type='text/javascript' src='" + combinedUrl + "' ></script>");
            }

            // We'll use this method of appending these nodes to the doc in order to avoid
            // having to keep track of the index in the collection when creating nodes explicitly;
            var temp = new HtmlDocument();
            temp.LoadHtml(sb.ToString());

            foreach (HtmlNode n in temp.DocumentNode.ChildNodes)
            {
                body.AppendChild(n);
            }

            
            // Move all the inline scripts to the end of the body after the newly inserted combined script
            if (InlineScriptNodes != null)
            {
                foreach (HtmlNode iscript in InlineScriptNodes)
                {
                    iscript.ParentNode.RemoveChild(iscript);
                    body.AppendChild(iscript);
                    // minify it's content
                    //iscript.InnerHtml = data.CombinerService.MinifyJs(iscript.InnerText);
                }
            }
        }

    }
}
