using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace F4ToPokeys
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Construction/Destruction

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            TaskbarIcon.ShowBalloonTip(Translations.Main.ApplicationTitle, Translations.Main.ApplicationStartedBalloonTip, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }

        #endregion // Construction/Destruction

        #region ConfigurationDialog

        private void MenuItemConfigure_Click(object sender, RoutedEventArgs e)
        {
            ShowConfigurationDialog();
        }

        // Public so App.xaml.cs can call it during startup when the user has unchecked
        // "Start minimized" and we want the dialog visible as soon as the app launches.
        public void ShowConfigurationDialog()
        {
            if (configurationDialog == null)
            {
                MenuItemQuit.IsEnabled = false;
                configurationDialog = new ConfigurationDialog();
                configurationDialog.ShowDialog();
                configurationDialog = null;

                // If the dialog closed because the user clicked File > Quit, the app
                // is shutting down and poking UI properties at this point can throw
                // from inside WPF's style/trigger machinery as resources are torn down.
                // Check ShutdownMode state via Dispatcher.HasShutdownStarted.
                if (System.Windows.Application.Current != null
                    && !Dispatcher.HasShutdownStarted
                    && MenuItemQuit != null)
                {
                    MenuItemQuit.IsEnabled = true;
                }
            }
            else
            {
                configurationDialog.Activate();
            }
        }

        private ConfigurationDialog configurationDialog;

        #endregion // ConfigurationDialog

        #region Quit

        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            // Save first so the tray-Quit path behaves the same as File > Quit in the
            // dialog: the user's work is preserved regardless of which Quit they use.
            try
            {
                ConfigHolder.Singleton.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Translations.Main.ConfigSaveErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                // Still proceed to quit on save failure — user chose Quit.
            }

            ConfigHolder.Singleton.Configuration.Dispose();

            if (configurationDialog == null)
                Close();
        }

        #endregion // Quit
    }
}
