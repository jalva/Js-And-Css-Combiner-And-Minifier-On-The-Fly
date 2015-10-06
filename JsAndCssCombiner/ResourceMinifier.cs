namespace JsAndCssCombiner
{
    /// <summary>
    /// This is a YUI Compressor based minifier
    /// </summary>
    public class ResourceMinifier : IResourceMinifier
    {
        public string MinifyJs(string js)
        {
            return Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(js);
        }

        public string MinifyCss(string css)
        {
            return Yahoo.Yui.Compressor.CssCompressor.Compress(css);
        }
    }
}
