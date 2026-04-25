using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.IO;

namespace F4ToPokeys
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Uri DarkThemeUri = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
        private static readonly Uri LightThemeUri = new Uri("Themes/LightTheme.xaml", UriKind.Relative);

        #region Construction/Destruction

        //static App()
        //{
        //    // To test localization
        //    System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("en-US");
        //    System.Threading.Thread.CurrentThread.CurrentUICulture = cultureInfo;
        //}

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!checkUniqueInstance())
            {
                Shutdown();
                return;
            }

            // enableWpfTrace();  // uncomment to log binding/resource errors to %LocalAppData%\F4ToPokeys\WpfTrace.txt

            // Load user-local preferences (theme, sampling interval, last-opened path).
            // These are always stored in preferences.xml, never in the portable Configuration file.
            UserPreferences prefs = UserPreferences.Load();

            // Apply theme before showing any window so there's no flash on the configured preference.
            ApplyTheme(prefs.IsDarkTheme);

            // Apply sampling interval (FalconConnector reads this directly).
            FalconConnector.Singleton.ReadFalconDataTimerInterval =
                TimeSpan.FromMilliseconds(prefs.ReadFalconDataTimerIntervalMS);

            // Also mirror preferences onto the in-memory Configuration so the UI's bindings read them.
            if (ConfigHolder.Singleton.Configuration != null)
                ConfigHolder.Singleton.Configuration.IsDarkTheme = prefs.IsDarkTheme;

            // Load last-opened config file if it still exists, otherwise fall back to default.
            string target = !string.IsNullOrEmpty(prefs.LastOpenedConfigPath)
                            && System.IO.File.Exists(prefs.LastOpenedConfigPath)
                ? prefs.LastOpenedConfigPath
                : ConfigHolder.DefaultConfigFileName;

            try
            {
                ConfigHolder.Singleton.LoadFrom(target);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, Translations.Main.ConfigLoadErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Re-apply IsDarkTheme on the newly-loaded Configuration so the VM binds consistently.
            if (ConfigHolder.Singleton.Configuration != null)
                ConfigHolder.Singleton.Configuration.IsDarkTheme = prefs.IsDarkTheme;

            FalconConnector.Singleton.start();

            MainWindow mainWindow = new MainWindow();
            MainWindow = mainWindow;
            MainWindow.Show();

            // If the user's preference is to NOT start minimized, auto-show the config
            // dialog right after the tray host is up, mimicking a right-click → Configure.
            if (!prefs.StartMinimized)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainWindow.ShowConfigurationDialog();
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        }

        #endregion // Construction/Destruction

        #region Theme

        public void ApplyTheme(bool dark)
        {
            if (Resources == null || Resources.MergedDictionaries.Count == 0)
                return;

            Uri target = dark ? DarkThemeUri : LightThemeUri;

            ResourceDictionary current = Resources.MergedDictionaries[0];
            if (current?.Source == target)
                return;

            ResourceDictionary next = new ResourceDictionary { Source = target };
            Resources.MergedDictionaries[0] = next;
        }

        #endregion

        #region uniqueInstanceMutex

        private bool checkUniqueInstance()
        {
            uniqueInstanceMutex = new System.Threading.Mutex(false, "F4ToPokeys");
            return uniqueInstanceMutex.WaitOne(TimeSpan.FromSeconds(0), false);
        }

        private System.Threading.Mutex uniqueInstanceMutex;

        #endregion // uniqueInstanceMutex

        #region WpfTrace

        private void enableWpfTrace()
        {
            try
            {
                Directory.CreateDirectory(ConfigHolder.AppDataPath);
                string traceFile = Path.Combine(ConfigHolder.AppDataPath, "WpfTrace.txt");

                TextWriterTraceListener listener = new TextWriterTraceListener(traceFile);
                listener.TraceOutputOptions = TraceOptions.None;
                Trace.Listeners.Add(listener);
                Trace.AutoFlush = true;

                Trace.WriteLine(string.Format("=== WPF trace started {0} ===", DateTime.Now));

                // Wire WPF presentation trace sources to the same Trace pipeline so
                // data-binding / resource / markup errors land in WpfTrace.txt.
                System.Diagnostics.PresentationTraceSources.Refresh();
                AttachListener(System.Diagnostics.PresentationTraceSources.DataBindingSource, listener);
                AttachListener(System.Diagnostics.PresentationTraceSources.ResourceDictionarySource, listener);
                AttachListener(System.Diagnostics.PresentationTraceSources.MarkupSource, listener);
                AttachListener(System.Diagnostics.PresentationTraceSources.DependencyPropertySource, listener);
            }
            catch
            {
                // Never let tracing setup break startup
            }
        }

        private static void AttachListener(TraceSource source, TraceListener listener)
        {
            if (source == null)
                return;
            source.Listeners.Add(listener);
            source.Switch.Level = SourceLevels.All;
        }

        #endregion

#region CrashLog

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string crashLogFileName = Path.Combine(ConfigHolder.AppDataPath, "CrashLog.txt");

            Directory.CreateDirectory(ConfigHolder.AppDataPath);

            using (StreamWriter streamWriter = new StreamWriter(crashLogFileName, append: true))
            {
                streamWriter.WriteLine(string.Format("{0}: {1}", DateTime.Now, e.Exception));
                streamWriter.WriteLine();
            }
        }

        #endregion // CrashLog
    }
}
