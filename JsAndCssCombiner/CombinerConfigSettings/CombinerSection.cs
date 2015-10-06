using System.Configuration;

namespace JsAndCssCombiner.CombinerConfigSettings
{
    public class CombinerSection : ConfigurationSection
    {
        [ConfigurationProperty("comboScriptUrl")]
        public string ComboScriptUrl
        {
            get { return (string) this["comboScriptUrl"]; }
        }

        [ConfigurationProperty("combinerLiveSettingsFile")]
        public string CombinerLiveSettingsFile
        {
            get { return (string) this["combinerLiveSettingsFile"]; }
        }

        [ConfigurationProperty("imagesCdnHostToPrepend")]
        public string ImagesCdnHostToPrepend
        {
            get { return (string)this["imagesCdnHostToPrepend"]; }
        }
    }
}
