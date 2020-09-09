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
using System.Windows.Shapes;

namespace F4ToPokeys
{
    /// <summary>
    /// Logique d'interaction pour ConfigurationDialog.xaml
    /// </summary>
    public partial class ConfigurationDialog : Window
    {
        public ConfigurationDialog()
        {
            InitializeComponent();
            DataContext = new ConfigurationViewModel(this);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigHolder.Singleton.Save();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, Translations.Main.ConfigSaveErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
