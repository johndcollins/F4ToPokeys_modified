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
            Loaded += ConfigurationDialog_Loaded;
        }

        private void ConfigurationDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ConfigurationDialog_Loaded;

            // Warm the PoKeys DataTemplate at Background priority so the dialog is
            // already visible before the ~1s first-materialization cost kicks in.
            // PrewarmHost is a zero-size hidden ContentPresenter; assigning a PoKeys
            // instance forces implicit-DataType dispatch to resolve the Views/PoKeysTemplate
            // DataTemplate, which triggers BAML parse + ControlTemplate wiring.
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (PrewarmHost == null) return;
                    PoKeys throwaway = new PoKeys();
                    PrewarmHost.Content = throwaway;

                    // After the layout pass has materialized the template, clear content
                    // and dispose the throwaway so its PoVID6066.FalconConnector event
                    // subscriptions don't linger.
                    Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            if (PrewarmHost != null)
                                PrewarmHost.Content = null;
                            throwaway.Dispose();
                        }),
                        System.Windows.Threading.DispatcherPriority.ContextIdle);
                }),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        private void DeviceTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ConfigurationViewModel vm = DataContext as ConfigurationViewModel;
            if (vm != null)
                vm.SelectedDevice = e.NewValue;
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
