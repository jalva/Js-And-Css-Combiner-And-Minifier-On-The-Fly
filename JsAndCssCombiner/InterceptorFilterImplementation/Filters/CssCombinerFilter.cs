using System;
using HtmlAgilityPack;
using System.Linq;
using System.Text;

namespace JsAndCssCombiner.InterceptorFilterImplementation.Filters
{
    public class CssCombinerFilter : BaseFilter
    {
        /// <summary>
        /// Replaces all local css tags on the page with a tag pointing to the combined resources
        /// of the tags removed. Appends the combined resources tag to the head of the document.
        /// Versions the resulting combined css tag (for performance reasons we're combining this operation in this filter).
        /// </summary>
        protected override void Process2(ref CombinerFilterContext data)
        {
            if (!data.CombineCss)
                return;

            if (CssNodes == null)
                return;

            var cssNodes = CssNodes.ToList();

            // Url pointing to the combined css of the tags removed
            // We're sending only distinct urls in order to eliminate duplicate loads of the same file...
            string[] combinedCssUrls = 
                data.CombinerService.GetCombinedCssUrls(
                   data.RequestPath,
                   cssNodes.Select(n => n.Attributes["href"].Value).Distinct().ToList(),
                   data.CssVersion,
                   data.SharedVersion,
                   data.CombinedResourcesUrl,
                   data.MinifyCss,
                   data.PrependCdnHostToImages);

            // Remove all css tags
            foreach (HtmlNode css in cssNodes)
                css.ParentNode.RemoveChild(css, false);

            // Insert the new combined css tag into the head element
            var head = Doc.DocumentNode.SelectSingleNode("//head");
            if (head == null)
                throw new ApplicationException("Head element in dom is null(TagsParser)");

            var sb = new StringBuilder();
            foreach (string combinedCssUrl in combinedCssUrls)
            {
                sb.Append("<link rel='stylesheet' type='text/css' href='" + combinedCssUrl + "' />");
            }

            // We'll use this method of appending these nodes to the doc in order to avoid
            // having to keep track of the index in the collection when creating nodes explicitly;
            var temp = new HtmlDocument();
            temp.LoadHtml(sb.ToString());

            foreach (HtmlNode n in temp.DocumentNode.ChildNodes)
            {
                head.PrependChild(n);
            }
        }
    }
}
