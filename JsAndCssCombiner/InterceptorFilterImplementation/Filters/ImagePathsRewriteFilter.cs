using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace JsAndCssCombiner.InterceptorFilterImplementation.Filters
{
    public class ImagePathsRewriteFilter : BaseFilter
    {
        public IEnumerable<HtmlNode> Images
        {
            get
            {
                var images = Doc.DocumentNode.SelectNodes(@"//img[@src and not(starts-with(normalize-space(@src), ""http""))]");
                if(images != null && images.Count > 0)
                    return images.Cast<HtmlNode>();
                return null;
            }
        }

        protected override void Process2(ref CombinerFilterContext data)
        {
            if (!data.PrependCdnHostToImages || string.IsNullOrEmpty(data.CdnHostToPrepend))
                return;

            if (Images == null)
                return;

            foreach (HtmlNode img in Images)
            {
                var url = img.Attributes["src"].Value;
                if(string.IsNullOrEmpty(url) || url.StartsWith("http"))
                    continue;

                // correct the image path relative to the requested page path (if doesn't start with /cms)
                string correctedImgPath = ImagePathsUtility.CorrectUrl(url, data.RequestPath);

                url = string.Format("{0}/{1}", data.CdnHostToPrepend, correctedImgPath.TrimStart('/'));

                img.Attributes["src"].Value = url;
            }
        }
    }
}
