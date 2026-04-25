using System;
using System.IO;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    // User-local preferences that are NOT part of a shared/portable config file:
    // theme choice, Falcon data sampling interval, and the path of the config file
    // most recently opened. Stored in its own XML next to F4ToPokeys.xml so that
    // exporting a Configuration file for another user never leaks personal prefs.
    public class UserPreferences
    {
        public bool IsDarkTheme { get; set; } = true;

        public double ReadFalconDataTimerIntervalMS { get; set; } = 100.0;

        // Absolute path of the config file that should be loaded at app startup.
        // Null/empty → use the default F4ToPokeys.xml in AppDataPath.
        public string LastOpenedConfigPath { get; set; }

        // When true, app launches to the tray without auto-opening the configuration
        // dialog. When false, the config dialog is shown at startup. Default true to
        // preserve the original tray-only behaviour for fresh installs.
        public bool StartMinimized { get; set; } = true;

        private static string PreferencesFileName =>
            Path.Combine(ConfigHolder.AppDataPath, "preferences.xml");

        public static UserPreferences Load()
        {
            try
            {
                if (!File.Exists(PreferencesFileName))
                    return new UserPreferences();

                XmlSerializer xs = new XmlSerializer(typeof(UserPreferences));
                using (Stream file = File.OpenRead(PreferencesFileName))
                    return (UserPreferences)xs.Deserialize(file);
            }
            catch
            {
                return new UserPreferences();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigHolder.AppDataPath);
                XmlSerializer xs = new XmlSerializer(typeof(UserPreferences));
                using (Stream file = File.Create(PreferencesFileName))
                    xs.Serialize(file, this);
            }
            catch
            {
                // Preferences are best-effort; never fail the app over them.
            }
        }
    }
}
