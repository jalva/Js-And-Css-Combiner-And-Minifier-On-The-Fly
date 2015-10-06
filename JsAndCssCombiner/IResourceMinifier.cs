namespace JsAndCssCombiner
{
    public interface IResourceMinifier
    {
        string MinifyJs(string js);
        string MinifyCss(string css);
    }
}
