using System;

namespace JsAndCssCombiner.LiveSettingsService
{
    public interface ILiveSettingsService
    {
        void InitializeSettingsForFile(Type stronglyTypedSettingsObjType, string settingsFilePath);
    }
}
