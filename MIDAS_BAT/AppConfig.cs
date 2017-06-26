using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MIDAS_BAT
{
    class AppConfig
    {
        private static readonly AppConfig instance = new AppConfig();
        public static AppConfig Instance
        {
            get
            {
                return instance;
            }
        }

        public bool? ShowTargetWord
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return (bool?)localSettings.Values["useShowTargetWord"];
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["useShowTargetWord"] = value;
            }
        }

        public bool? UseHandWritingRecognition
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return (bool?)localSettings.Values["useHandWritingRecognition"];
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["useHandWritingRecognition"] = value;
            }
        }

        public bool? UseJamoSeperation
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return (bool?)localSettings.Values["useJamoSeperation"];
            }
            set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["useJamoSeperation"] = value;
            }
        }

        private AppConfig()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["showTargetWord"] == null)
                localSettings.Values["showTargetWord"] = (bool?)false;
            if (localSettings.Values["useJamoSeperation"] == null)
                localSettings.Values["useJamoSeperation"] = (bool?)false;
            if (localSettings.Values["useHandWritingRecognition"] == null)
                localSettings.Values["useHandWritingRecognition"] = (bool?)false;
        }
    }
}
