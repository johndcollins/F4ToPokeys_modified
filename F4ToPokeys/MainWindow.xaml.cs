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
            if (configurationDialog == null)
            {
                MenuItemQuit.IsEnabled = false;
                configurationDialog = new ConfigurationDialog();
                configurationDialog.ShowDialog();
                configurationDialog = null;
                MenuItemQuit.IsEnabled = true;
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
            if (configurationDialog == null)
                Close();
        }

        #endregion // Quit
    }
}
