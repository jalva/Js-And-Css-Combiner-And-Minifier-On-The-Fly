using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web;
using JsAndCssCombiner.CombinerServices;

namespace JsAndCssCombiner
{
    class CombinedResourceHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get { return false; }
        }
        
        public void ProcessRequest(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            bool isIe =  HttpContext.Current.Request.Browser.Browser.Trim()
                .Equals("IE", StringComparison.InvariantCultureIgnoreCase);
            int ieVersion = isIe ? HttpContext.Current.Request.Browser.MajorVersion : 0;

            ICombinerService myCombiner = CombinerServiceFactory.CreateCombinerService();
            
            string imagesHostToPrepend = CombinerConstantsAndSettings.WebSettings.ImagesCdnHostToPrepend;

            byte[] combinedContent = null;

            try
            {
                // *** get the combined content to write to the response
                combinedContent = myCombiner.ServeCombinedContent(ieVersion, context.Request.QueryString,
                                                                         HttpContext.Current.Server.MapPath,
                                                                         File.ReadAllText, imagesHostToPrepend);
                // *******
            }
            catch
            {
                combinedContent = Encoding.UTF8.GetBytes("/* no content */");
            }

            // *** write combined content into the response stream
            WriteBytes(context, combinedContent);
            // *******
            
        }

        private void WriteBytes(HttpContext context, byte[] bytes)
        {
            bool isGzipped = CanGZipOrDeflate(context.Request, "gzip");
            bool isDeflated = CanGZipOrDeflate(context.Request, "deflate");

            string typeQs = context.Request.QueryString[CombinerConstantsAndSettings.TypeUrlKey];
            var type = (CombinedResourceType)Enum.Parse(typeof(CombinedResourceType), typeQs.ToLower());

            HttpResponse response = context.Response;

            string contentType = type == CombinedResourceType.css ? @"text/css" : @"text/javascript";
            response.ContentType = contentType;


            // Determine which compression mode to use
            if (isGzipped)
            {
                response.Filter = new GZipStream(context.Response.Filter, CompressionMode.Compress);
                response.AppendHeader("Content-Encoding", "gzip");
            }
            else if (isDeflated)
            {
                response.Filter = new DeflateStream(context.Response.Filter, CompressionMode.Compress);
                response.AppendHeader("Content-Encoding", "deflate");
            }
            else
            {
                response.AppendHeader("Content-Length", bytes.Length.ToString());
                //response.AppendHeader("Content-Encoding", "utf-8");
            }
           
            response.ContentEncoding = Encoding.Unicode;

            // Allow proxy servers to cache encoded and unencoded versions separately
            response.AppendHeader("Vary", "Content-Encoding");


            // Set the response client cacheability
            response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.Add(CombinerConstantsAndSettings.CacheDuration));
            context.Response.Cache.SetValidUntilExpires(true);
            context.Response.Cache.SetMaxAge(CombinerConstantsAndSettings.CacheDuration);

            // Add akamai's header
            var akamaiHeader = String.Format("cache-maxage={0}m,!no-store,!bypass-cache", 30);
            context.Response.AddHeader("Edge-control", akamaiHeader);

            try
            {
                // *** write the bytes to the response
                response.OutputStream.Write(bytes, 0, bytes.Length);
                // *****
                //response.Flush();
            }
            catch
            {
            }
        }

        private bool CanGZipOrDeflate(HttpRequest request, string encodingHeaderValue)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (!string.IsNullOrEmpty(acceptEncoding) && (acceptEncoding.Contains(encodingHeaderValue)))
                return true;
            return false;
        }
    }
}
