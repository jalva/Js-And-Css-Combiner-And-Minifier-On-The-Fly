using System;
using System.Configuration;
using System.Reflection;
using JsAndCssCombiner.CombinerConfigSettings;

namespace JsAndCssCombiner
{
    /// <summary>
    /// Constants and Settings related to the Combiner.
    /// Includes web.config related settings
    /// </summary>
    public static class CombinerConstantsAndSettings
    {
        #region query string parameters used in the combined url

        #region query string keys

        public const string PageUrlKey = "p";
        public const string VersionUrlKey = "v";
        public const string SharedVersionUrlKey = "v2";
        public const string FilesUrlKey = "urls";
        public const string TypeUrlKey = "t";
        public readonly static TimeSpan CacheDuration = TimeSpan.FromDays(30);
        public const string MinifyUrlKey = "m";
        public const string RewriteImagePathsUrlKey = "rw";

        #endregion

        /// <summary>
        /// Web.config settings related to the combiner
        /// </summary>
        public static readonly CombinerSection WebSettings =
            (CombinerSection)ConfigurationManager.GetSection(
                "JACombinerAndOptimizerGroup/combinerSettings");

        /// <summary>
        /// This is the version number shared by both Js and Css resources
        /// (it is tied to the build version and is dynamically set by the app)
        /// (used in order to force a client refresh after deployments)
        /// </summary>
        public static readonly string JsAndCssSharedVersion;

        #endregion


        static CombinerConstantsAndSettings()
        {
            // Initialize the js and css shared version based on the assembly version
            // AssemblyInfo.cs must have: [assembly: AssemblyVersion("1.0.0.*")] for this number to change with every build
            try
            {
                JsAndCssSharedVersion = Assembly.GetExecutingAssembly().GetName().Version.Revision.ToString();
            }
            catch (Exception)
            {
                JsAndCssSharedVersion = "none";
            }
        }
    }
}
