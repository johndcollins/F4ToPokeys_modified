using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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

            try
            {
                ConfigHolder.Singleton.Load();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, Translations.Main.ConfigLoadErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            FalconConnector.Singleton.start();
        }

        #endregion // Construction/Destruction

        #region uniqueInstanceMutex

        private bool checkUniqueInstance()
        {
            uniqueInstanceMutex = new System.Threading.Mutex(false, "F4ToPokeys");
            return uniqueInstanceMutex.WaitOne(TimeSpan.FromSeconds(0), false);
        }

        private System.Threading.Mutex uniqueInstanceMutex;

        #endregion // uniqueInstanceMutex

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
