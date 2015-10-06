using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Specialized;
using JsAndCssCombiner.CacheService;
using JsAndCssCombiner.LoggingService;

namespace JsAndCssCombiner.CombinerServices
{
    public class LongUrlCombinerService : ICombinerService
    {
        private readonly ICacheService _cacheService;
        private readonly IResourceMinifier _minifier;
        private readonly ILoggingService _logger;

        // This dictionary contains locks used to retrieve/insert the combined content for each page
        // This is a more granular way of locking to avoid locking requests that have nothing to do with this entry
        private static readonly ConcurrentDictionary<string, AutoResetEvent> LocksForCombinedContent =
            new ConcurrentDictionary<string, AutoResetEvent>();

        public LongUrlCombinerService(ICacheService cacheService, IResourceMinifier minifier, ILoggingService logger)
        {
            _cacheService = cacheService;
            _minifier = minifier;
            _logger = logger;
        }

        public string[] GetCombinedScriptUrls(string pageUrl, IList<string> scriptUrls, string manualVersion, string sharedVersion, string combinedHandlerUrl, bool minifyJs, bool rewriteImagePaths)
        {
            return GetCombinedUrls(pageUrl, scriptUrls, manualVersion, sharedVersion, CombinedResourceType.javascript, combinedHandlerUrl, minifyJs, rewriteImagePaths);
        }

        public string[] GetCombinedCssUrls(string pageUrl, IList<string> cssUrls, string manualVersion, string sharedVersion, string combinedHandlerUrl, bool minifyCss, bool rewriteImagePaths)
        {
            return GetCombinedUrls(pageUrl, cssUrls, manualVersion, sharedVersion, CombinedResourceType.css, combinedHandlerUrl, minifyCss, rewriteImagePaths);
        }

        public string GetVersionQueryString(string manualVersion, string sharedVersion)
        {
            return CombinerConstantsAndSettings.VersionUrlKey + "=" + manualVersion +
                   "&" + CombinerConstantsAndSettings.SharedVersionUrlKey + "=" + sharedVersion;
        }

        public byte[] ServeCombinedContent(int ieVersion, NameValueCollection queryStringParms, Func<string, string> pathMapper, Func<string, string> fileReader, string imagesHostToPrepend)
        {
            string manualVersion = queryStringParms[CombinerConstantsAndSettings.VersionUrlKey];
            string sharedVersion = queryStringParms[CombinerConstantsAndSettings.SharedVersionUrlKey];
            string type = queryStringParms[CombinerConstantsAndSettings.TypeUrlKey];
            string encodedFileUrls = queryStringParms[CombinerConstantsAndSettings.FilesUrlKey];
            var resourceType = (CombinedResourceType) Enum.Parse(typeof (CombinedResourceType), type.ToLower());
            string minifyQs = queryStringParms[CombinerConstantsAndSettings.MinifyUrlKey];
            bool minify;
            if (!bool.TryParse(minifyQs, out minify))
                minify = true; // default value is true
            string rewriteImagePathsQs = queryStringParms[CombinerConstantsAndSettings.RewriteImagePathsUrlKey];
            bool rewriteImagePaths;
            if(!bool.TryParse(rewriteImagePathsQs, out rewriteImagePaths))
                rewriteImagePaths = false;// default value is false
            

            var cacheKey = GetCacheKeyForCombinedContent(manualVersion, sharedVersion, resourceType, ieVersion, encodedFileUrls.GetHashCode().ToString(), minify, rewriteImagePaths);
            var syncRoot = LocksForCombinedContent.GetOrAdd(cacheKey, new AutoResetEvent(true));
            var mappedFiles = new List<string>();


            var result = _cacheService.Get(
                syncRoot,
                cacheKey,
                () =>
                    {
                        var allScripts = new StringBuilder();
                        var unencryptedFileUrlsCsv = DecodeFrom64(encodedFileUrls);
                        var fileUrlsList = unencryptedFileUrlsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string fileName in fileUrlsList)
                        {
                            string filePath;
                            string fileName2 = null;
                            try
                            {
                                // Remove any query string from fileName
                                int indx = fileName.IndexOf("?");
                                fileName2 = fileName.Substring(0, (indx > 0 ? indx : fileName.Length));

                                // ----- Important: only allow files with extensions '.js' and '.css'!
                                if(!fileName2.ToLower().EndsWith(".js") && !fileName2.EndsWith(".css"))
                                    continue;
                                // -----
                                
                                filePath = pathMapper(fileName2);

                                mappedFiles.Add(filePath);
                            }
                            catch (Exception e)
                            {
                                _logger.Error("Exception mapping combined file path: " + fileName2 + ". Exception: " + e.Message);
                                continue;
                            }

                            string content = "/*no content: " + filePath + "*/"; // this can be used for debugging purposes
                            try
                            {
                                content = fileReader(filePath); // File.ReadAllText(filePath);
                            }
                            catch (Exception rExc)
                            {
                                _logger.Error("Exception reading resource: " + fileName2 + ". Exception: " + rExc.Message);
                            }

                            // If file is already minified just append the content, else minify before appending
                            if (filePath.Contains(".min"))
                            {
                                allScripts.Append(content);
                            }
                            else
                            {
                                try
                                {
                                    string minified;
                                    if (resourceType == CombinedResourceType.javascript)
                                    {
                                        // ***** Minify the script file and remove comments and white spaces
                                        minified = minify ? _minifier.MinifyJs(content) : content;
                                        // ********
                                    }
                                    else
                                    {
                                        // ***** Convert all relative image paths to absolute paths
                                        content = FixImagePaths(content, fileName2, rewriteImagePaths, imagesHostToPrepend);
                                        // ********
                                        // ***** Minify the css file and remove comments and white spaces
                                        minified = minify ? _minifier.MinifyCss(content) : content;
                                        // ********
                                    }

                                    allScripts.Append(minified);
                                }
                                catch (Exception mExc)
                                {
                                    _logger.Error("Exception Minimizing resource: " + fileName2 + ". Exception: " + mExc.Message);
                                    allScripts.Append(content); // add non-minified content if minification failed
                                }
                            }
                        }

                        // *** Get the combined content in bytes
                        string minifiedAll = allScripts.ToString();
                        
                        if (string.IsNullOrEmpty(minifiedAll))
                            return null;

                        byte[] bytes = Encoding.UTF8.GetBytes(minifiedAll);
                        // ******
                        
                        return bytes;
                    },
                    () =>
                    {
                        var policy = new CacheItemPolicy { Priority = CacheItemPriority.Default };
                        policy.ChangeMonitors.Add(new HostFileChangeMonitor(mappedFiles));
                        return policy;
                    }
            );

            return result;
        }

        public string MinifyJs(string js)
        {
            return _minifier.MinifyJs(js);
        }

        public string MinifyCss(string css)
        {
            return _minifier.MinifyCss(css);
        }

        #region Private methods

        private string[] GetCombinedUrls(string pageUrl, IList<string> resUrls, string manualVersion, string sharedVersion, CombinedResourceType resType, string combinedHandlerUrl, bool minify, bool rewriteImagePaths)
        {

            // Partition the urls csv into multiple csv strings if necessary in order to produce urls 
            // that are of not greater than 2083 chars to avoid any browser and/or server issues;
            var finalList = new List<string>();
            var list = new List<string>();
            int charCount = combinedHandlerUrl.Count();
            foreach(string url in resUrls)
            {
                charCount += url.Count();
                
                if(charCount >= 1300)
                {
                    finalList.Add(string.Join(",", list));
                    list = new List<string>();
                    charCount = combinedHandlerUrl.Count();
                }
                list.Add(url);
            }

            finalList.Add(string.Join(",", list));

            var result = new List<string>();

            foreach (string urlsCsv in finalList)
            {
                if (resType == CombinedResourceType.javascript)
                    result.Add( combinedHandlerUrl +
                           BuildQueryStringForCombinedUrl(pageUrl, manualVersion, sharedVersion,
                                                          CombinedResourceType.javascript, urlsCsv, minify, rewriteImagePaths));

                if (resType == CombinedResourceType.css)
                    result.Add( combinedHandlerUrl +
                           BuildQueryStringForCombinedUrl(pageUrl, manualVersion, sharedVersion,
                                                          CombinedResourceType.css, urlsCsv, minify, rewriteImagePaths));
            }

            return result.ToArray();
        }

        private string BuildQueryStringForCombinedUrl(string pageUrl, string manualVersion, string sharedVersion, CombinedResourceType type, string fileUrls, bool minify, bool rewriteImagePaths)
        {
            return "?" + CombinerConstantsAndSettings.PageUrlKey + "=" + pageUrl +
                   "&" + GetVersionQueryString(manualVersion, sharedVersion) +
                   "&" + CombinerConstantsAndSettings.TypeUrlKey + "=" + type +
                   "&" + CombinerConstantsAndSettings.MinifyUrlKey + "=" + minify +
                   "&" + CombinerConstantsAndSettings.RewriteImagePathsUrlKey + "=" + rewriteImagePaths +
                   "&" + CombinerConstantsAndSettings.FilesUrlKey + "=" + EncodeTo64(fileUrls);
        }

        /// <summary>
        /// The cache key will be based on:
        /// - the version number
        /// - the type (javascript or css)
        /// - whether content is compressed or not
        /// - the hashcode of the file names in this group
        /// - the Internet Explorer version (since there might be additional files for different ie versions)
        /// </summary>
        /// <returns></returns>
        private static string GetCacheKeyForCombinedContent(string manualVersion, string sharedVersion, CombinedResourceType type, int ieVersion, string urlsHash, bool minify, bool rewriteImagesPaths)
        {
            return "CombdResrc" +
                    ".mv=" + manualVersion +
                    ".sv=" + sharedVersion +
                    ".t=" + type +
                    ".ie=" + ieVersion +
                    ".m=" + minify +
                    ".rw=" + rewriteImagesPaths +
                    ".hash=" + urlsHash;
        }

        /// <summary>
        /// Corrects all relative image paths inside a style sheet to turn them into full paths
        /// in order to allow for these style sheets to work when combined into one file.
        /// Also, if 'rewriteImagePaths' is true, prepends a subdomain to all image paths in the css file.
        /// </summary>
        /// <param name="cssContent">The contents of the css file</param>
        /// <param name="cssFilePath">The path of the css file</param>
        /// <param name="rewriteImagePaths">If true will prepend a subdomain to all image paths in this css</param>
        /// <param name="imagesHostToPrepend">The subdomain to prepend to the image paths</param>
        /// <returns></returns>
        private static string FixImagePaths(string cssContent, string cssFilePath, bool rewriteImagePaths, string imagesHostToPrepend)
        {
            var processedUrls = new Dictionary<string, string>();
            var imgRgx = new Regex(@"(?<=url\().*?\)");
            MatchCollection imgMatches = imgRgx.Matches(cssContent);
            foreach (Match imgM in imgMatches)
            {
                string url = imgM.ToString().Replace(")", "").Replace("\"", "").Replace("'", "").Trim();

                if (string.IsNullOrEmpty(url) || processedUrls.ContainsKey(url))
                    continue; 

                processedUrls.Add(url, url);

                var rgxUsedToReplaceInCss = new Regex(@"\(\s*[""']{0,1}" + url + @"[""|']{0,1}\s*\)");

                // correct the image path relative to the cssFilePath (if doesn't start with /cms)
                string correctedImgPath = ImagePathsUtility.CorrectUrl(url, cssFilePath);

                if (rewriteImagePaths && !ImagePathsUtility.IsFont(url)) 
                {
                    // forward slash in the beginning not required here, since this will be an absolute path
                    correctedImgPath = string.Format("{0}/{1}", imagesHostToPrepend, correctedImgPath.TrimStart('/'));
                }
                
                cssContent = rgxUsedToReplaceInCss.Replace(cssContent, "(" + correctedImgPath + ")");
            }

            return cssContent;
        }


        private static string EncodeTo64(string toEncode)
        {

            byte[] toEncodeAsBytes

                  = Encoding.ASCII.GetBytes(toEncode);

            string returnValue

                  = Convert.ToBase64String(toEncodeAsBytes);

            return returnValue;

        }

        private static string DecodeFrom64(string encodedData)
        {
            try
            {
                byte[] encodedDataAsBytes

                    = Convert.FromBase64String(encodedData);

                string returnValue =

                    Encoding.ASCII.GetString(encodedDataAsBytes);

                return returnValue;
            }
            catch
            {
                return string.Empty;
            }

        }

        #endregion
    }
}
