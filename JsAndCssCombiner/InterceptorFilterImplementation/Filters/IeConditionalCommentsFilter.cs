using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace JsAndCssCombiner.InterceptorFilterImplementation.Filters
{
    public class IeConditionalCommentsFilter : BaseFilter
    {
        /// <summary>
        /// Removes all IE conditional comments and if the IE versions match
        /// injects the content back into the dom (to allow the subsequent parsing to include that content)
        /// </summary>
        protected override void Process2(ref CombinerFilterContext data)
        {
            var ieVersion = data.IeVersion;

            if (!data.CombineJs && !data.CombineCss)
                return;

            if (IeConditionalComments == null)
                return;

            foreach (HtmlNode comment in IeConditionalComments)
            {
                // If not an IE browser just remove comment
                if (ieVersion <= 0)
                    comment.ParentNode.RemoveChild(comment);
                else
                {
                    bool addComment = false;
                    // Ese if it is an IE browser then check if the versions match
                    // if they don't, then remove comment
                    var rgx = new Regex(@"IE \d");
                    var match = rgx.Match(comment.InnerHtml);
                    if (match.Success)
                    {
                        int ieV = int.Parse(match.Value.Replace("IE ", ""));
                        if (ieV != ieVersion)
                            // If not the same IE version just remove comment
                            comment.ParentNode.RemoveChild(comment);
                        else
                            addComment = true;
                    }
                    else
                        // If comment doesn't have a version, then it applies to all IE versions
                        addComment = true;

                    if (addComment)
                    {
                        // Remove the start and end comment tags of this comment
                        string content = comment.InnerHtml;
                        rgx = new Regex(@"\<!--\[if.*? IE *?\d* *?]>");
                        content = rgx.Replace(content, "", 1);
                        content = content.Replace("<![endif]-->", "");
                        content = content.Trim();

                        // Remove the comment node and inject it's content back to the _doc
                        var temp = new HtmlDocument();
                        temp.LoadHtml(content);

                        var parentNode = comment.ParentNode;
                        int count = 0;
                        foreach (HtmlNode n in temp.DocumentNode.ChildNodes)
                        {
                            // This logic will ensure that the content inside the comments
                            // will be injected in the same place where the comment used to be;
                            if (count++ == 0)
                                parentNode.ReplaceChild(n, comment);
                            else
                                parentNode.InsertAfter(n, temp.DocumentNode.ChildNodes[count - 1]);
                        }

                    }
                }
            }
        }
    }
}
