using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;

namespace JsAndCssCombiner
{
    /// <summary>
    /// Utility class used to correct image paths relative to the requested page or css where they originate from
    /// </summary>
    public static class ImagePathsUtility
    {
        /// <summary>
        /// Corrects the image path relative to the requested page or css where they originate from
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestPath"></param>
        /// <returns></returns>
        public static string CorrectUrl(string url, string requestPath)
        {
            var correctedImgPath = url.TrimStart('~');

            // make sure 'requestPath' starts with a '/'
            if (!requestPath.StartsWith("/"))
                requestPath = "/" + requestPath;
            
            if (!url.StartsWith("/") && !url.StartsWith("../"))
            {
                correctedImgPath = VirtualPathUtility.GetDirectory(requestPath) + url.TrimStart('/');
            }
            else if (url.StartsWith("../"))
            {
                var path = VirtualPathUtility.GetDirectory(requestPath);
                var regex = new Regex(@"\.\./");
                var levelUpMatches = regex.Matches(url);
                var numberOfLevelsUp = levelUpMatches.Count;
                var pathDirs = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var newPathDirs = pathDirs.Take(pathDirs.Length - numberOfLevelsUp);
                var newPath = String.Join("/", newPathDirs).Trim();
                correctedImgPath = regex.Replace(url, "");
                correctedImgPath = String.IsNullOrEmpty(newPath) ? String.Format("/{0}", correctedImgPath) : String.Format("/{0}/{1}", newPath, correctedImgPath);
            }

            return correctedImgPath;
        }


        public static bool IsFont(string url)
        {
            var urlLower = url.ToLower();
            if (urlLower.IndexOf(".eot") == -1 && urlLower.IndexOf(".woff") == -1 && urlLower.IndexOf(".ttf") == -1 && urlLower.IndexOf(".svg") == -1)
                return false;
            return true;
        }
    }
}
