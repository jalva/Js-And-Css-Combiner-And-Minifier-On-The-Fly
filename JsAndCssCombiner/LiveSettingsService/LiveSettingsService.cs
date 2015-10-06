using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Caching;
using System.Threading;
using System.Web.Script.Serialization;
using JsAndCssCombiner.CacheService;
using JsAndCssCombiner.LoggingService;

namespace JsAndCssCombiner.LiveSettingsService
{
    public class LiveSettingsService : ILiveSettingsService
    {
        private readonly ICacheService _cacheService;
        private static readonly ConcurrentDictionary<string, AutoResetEvent> Locks =
            new ConcurrentDictionary<string, AutoResetEvent>();

        private static readonly ILoggingService Logger = new LoggingService.LoggingService();

        public LiveSettingsService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public void InitializeSettingsForFile(Type stronglyTypedSettingsObjType, string settingsFilePath)
        {
            var cacheKey = "LiveSettings." + settingsFilePath;
            var syncRoot = Locks.GetOrAdd(settingsFilePath, new AutoResetEvent(true));

            _cacheService.Get(
                syncRoot,
                cacheKey,
                () => InitializeSettingsObject(stronglyTypedSettingsObjType, settingsFilePath),
                () =>
                    {
                        var policy = new CacheItemPolicy { Priority = CacheItemPriority.Default, RemovedCallback = args => InitializeSettingsForFile(stronglyTypedSettingsObjType, settingsFilePath)};
                        policy.ChangeMonitors.Add(new HostFileChangeMonitor(new List<string> { settingsFilePath }));
                        return policy;
                    });

        }

        private static object InitializeSettingsObject(Type stronglyTypedSettingsObjType, string settingsFilePath)
        {
            Dictionary<string, object> settingsObj = null;
            try
            {
                string json = File.ReadAllText(settingsFilePath);
                settingsObj =
                    new JavaScriptSerializer().Deserialize(json, new Dictionary<string, object>().GetType()) as
                    Dictionary<string, object>;

                if (settingsObj == null)
                    throw new Exception();
            }
            catch(Exception e)
            {
                Logger.Error("Error in LiveSettingsService.cs; unable to deserialize file: " + settingsFilePath + ". Exception: " + e.Message);
                settingsObj = new Dictionary<string, object>();
            }

            var propertyList = stronglyTypedSettingsObjType.GetProperties();
            foreach (var propertyInfo in propertyList)
            {
                try
                {
                    if (!settingsObj.ContainsKey(propertyInfo.Name))
                        continue;

                    var val = settingsObj[propertyInfo.Name];
                    var convertedVal = Convert.ChangeType(val, propertyInfo.PropertyType);
                    propertyInfo.SetValue(stronglyTypedSettingsObjType, convertedVal, null);
                }
                catch (Exception e)
                {
                    Logger.Error("Error Initializing Live Settings for file: " + settingsFilePath + ". Exception: " + e.Message);
                }
            }

            return settingsObj;
        }
    }
}
