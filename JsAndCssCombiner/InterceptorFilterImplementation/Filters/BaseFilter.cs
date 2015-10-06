using System.Collections.Generic;
using HtmlAgilityPack;
using Persephone.Processing.Pipeline;
using System.Linq;
using System.Text.RegularExpressions;

namespace JsAndCssCombiner.InterceptorFilterImplementation.Filters
{
    public abstract class BaseFilter : IFilter<CombinerFilterContext>
    {
        protected HtmlDocument Doc;

        /// <summary>
        /// JavaScript include tags.
        /// Excludes the following scripts:
        ///     - the mbox js since it needs to load in the beginning of the page;
        ///     - the jquery js since it is used by the mbox scripts loaded in the beginning of the page;
        ///     - external scripts that start with 'http';
        /// </summary>
        public IEnumerable<HtmlNode> ScriptIncludeNodes
        {
            get
            {
                var regx = new Regex(@"jquery-\d.\d.\d(.min){0,1}.js");
                var result = Doc.DocumentNode.SelectNodes(@"//script[@src and not(starts-with(normalize-space(@src), ""http""))]");
                if(result == null || result.Count == 0)
                    return null;
                return result.Cast<HtmlNode>()
                    .Where(
                        n => !(n.Attributes["src"].Value.Contains("mbox.js") ||
                                regx.IsMatch(n.Attributes["src"].Value)));
            }
        }

        /// <summary>
        /// JavaScript inline tags.
        /// Excludes the following scripts:
        ///     - the ones that invoke the mboxCreate function, since these are mbox invocations that need to be in the beginning of the page;
        ///     - the ones that delcare the reatTTCookie function since it is used by the mbox invocations;
        /// </summary>
        public IEnumerable<HtmlNode> InlineScriptNodes
        {
            get
            {
                var result = Doc.DocumentNode.SelectNodes(@"//script[not(@src)]");
                if(result == null || result.Count == 0)
                    return null;
                return result.Cast<HtmlNode>()
                    .Where(
                        n => !(n.InnerText.Contains("mboxCreate") || 
                                n.InnerText.Contains("readTTCookie") ||
                                  n.Attributes["nocombine"] != null));
            }
        }

        /// <summary>
        /// Css link tags.
        /// Excludes the following tags:
        ///     - external css that starts with 'http';
        ///     - css that has 'type' attribute other than 'screen';
        /// </summary>
        public IEnumerable<HtmlNode> CssNodes
        {
            get
            {
                var result = Doc.DocumentNode.SelectNodes(
                    @"//link[@href and not(starts-with(normalize-space(@href), ""http"")) and (not(@media) or @media=""screen"" or @media=""all"")]");
                if(result == null || result.Count == 0)
                    return null;
                return result.Cast<HtmlNode>();
            }
        }

        /// <summary>
        /// IE conditional comments that are meant to inject JavaScript and Css specific for a particular version of IE.
        /// </summary>
        public IEnumerable<HtmlNode> IeConditionalComments
        {
            get
            {
                var result = Doc.DocumentNode.SelectNodes(@"//comment()[substring(.,1,5) =""<!--[""]");
                if(result == null || result.Count == 0)
                    return null;
                return result.Cast<HtmlNode>().ToList();
            }
        }

        public bool Process(ref CombinerFilterContext data)
        {
            Doc = data.Document;

            // to be overriden in super classes
            Process2(ref data);
            
            return true;
        }

        /// <summary>
        /// Template method to be overriden by subclasses.
        /// Each filter instance will put it's main processing logic here.
        /// </summary>
        /// <param name="data"></param>
        protected abstract void Process2(ref CombinerFilterContext data);

    }
}
