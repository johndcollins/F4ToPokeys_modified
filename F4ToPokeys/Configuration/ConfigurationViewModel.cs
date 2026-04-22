using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace F4ToPokeys
{
    public class ConfigurationViewModel : BindableObject
    {
        #region Construction/Destruction
        public ConfigurationViewModel(Window view)
        {
            this.view = view;
            SaveCommand = new RelayCommand(executeSave);
            LoadCommand = new RelayCommand(executeLoad);
            ToggleThemeCommand = new RelayCommand(executeToggleTheme);

            // Status dot reflects FalconConnector live state
            FalconConnector.Singleton.FalconStarted += OnFalconStarted;
            FalconConnector.Singleton.FalconStopped += OnFalconStopped;

            // Rebuild the tree whenever Configuration is replaced (Load)
            ConfigHolder.Singleton.PropertyChanged += OnConfigHolderPropertyChanged;

            isDarkTheme = ConfigHolder.Configuration?.IsDarkTheme ?? true;

            BuildDeviceGroups();
        }
        #endregion // Construction/Destruction

        #region Title
        public string Title
        {
            get
            {
                Version assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string title = string.Format("{0} {1}.{2}.{3}", Translations.Main.ApplicationTitle, assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);

#if DEBUG
                title += " debug";
#endif

                return title;
            }
        }
        #endregion

        #region ConfigHolder
        public ConfigHolder ConfigHolder { get { return ConfigHolder.Singleton; } }
        #endregion // ConfigHolder

        #region SaveCommand
        public RelayCommand SaveCommand { get; private set; }

        private void executeSave(object o)
        {
            try
            {
                ConfigHolder.Save();
            }
            catch (Exception e)
            {
                MessageBox.Show(view, e.Message, Translations.Main.ConfigSaveErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion // SaveCommand

        #region LoadCommand
        public RelayCommand LoadCommand { get; private set; }

        private void executeLoad(object o)
        {
            try
            {
                ConfigHolder.Load();
            }
            catch (Exception e)
            {
                MessageBox.Show(view, e.Message, Translations.Main.ConfigLoadErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion // LoadCommand

        #region FalconLightList (raw) + view (sorted, grouped)
        public List<FalconLight> FalconLightList
        {
            get { return FalconConnector.Singleton.LightList; }
        }

        private ICollectionView falconLightCollectionView;
        public ICollectionView FalconLightCollectionView
        {
            get
            {
                if (falconLightCollectionView == null)
                {
                    falconLightCollectionView = CollectionViewSource.GetDefaultView(FalconLightList);
                    falconLightCollectionView.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
                    falconLightCollectionView.SortDescriptions.Add(new SortDescription("Label", ListSortDirection.Ascending));
                    falconLightCollectionView.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
                }
                return falconLightCollectionView;
            }
        }
        #endregion

        #region FalconGaugeList (raw) + view (sorted)
        public List<FalconGauge> FalconGaugeList
        {
            get { return FalconConnector.Singleton.GaugeList; }
        }

        private ICollectionView falconGaugeCollectionView;
        public ICollectionView FalconGaugeCollectionView
        {
            get
            {
                if (falconGaugeCollectionView == null)
                {
                    falconGaugeCollectionView = CollectionViewSource.GetDefaultView(FalconGaugeList);
                    falconGaugeCollectionView.SortDescriptions.Add(new SortDescription("Label", ListSortDirection.Ascending));
                }
                return falconGaugeCollectionView;
            }
        }
        #endregion

        #region IsFalconConnected
        private bool isFalconConnected;
        public bool IsFalconConnected
        {
            get { return isFalconConnected; }
            private set
            {
                if (isFalconConnected == value)
                    return;
                isFalconConnected = value;
                RaisePropertyChanged(nameof(IsFalconConnected));
            }
        }

        private void OnFalconStarted(object sender, EventArgs e)
        {
            view.Dispatcher.BeginInvoke(new Action(() => IsFalconConnected = true));
        }

        private void OnFalconStopped(object sender, EventArgs e)
        {
            view.Dispatcher.BeginInvoke(new Action(() => IsFalconConnected = false));
        }
        #endregion // IsFalconConnected

        #region DeviceGroups
        public ObservableCollection<DeviceGroupViewModel> DeviceGroups { get; } = new ObservableCollection<DeviceGroupViewModel>();

        private void BuildDeviceGroups()
        {
            DeviceGroups.Clear();
            Configuration configuration = ConfigHolder.Configuration;
            if (configuration == null)
                return;

            DeviceGroups.Add(new DeviceGroupViewModel(
                Translations.Main.PoKeysConfigCaption,
                DeviceGroupKind.PoKeys,
                configuration.PoKeysList,
                configuration.AddPoKeysCommand,
                OnDeviceRemoved));

            DeviceGroups.Add(new DeviceGroupViewModel(
                Translations.Main.PololuMaestroConfigCaption,
                DeviceGroupKind.PololuMaestro,
                configuration.PololuMaestroList,
                configuration.AddPololuMaestroCommand,
                OnDeviceRemoved));

            DeviceGroups.Add(new DeviceGroupViewModel(
                Translations.Main.DEDuinoConfigCaption,
                DeviceGroupKind.DEDuino,
                configuration.DEDuinoList,
                configuration.AddDEDuinoCommand,
                OnDeviceRemoved));
        }

        public void OnDeviceRemoved(IList removed)
        {
            if (removed == null || SelectedDevice == null)
                return;

            foreach (object item in removed)
            {
                if (ReferenceEquals(item, SelectedDevice))
                {
                    SelectedDevice = null;
                    return;
                }
            }
        }
        #endregion // DeviceGroups

        #region SelectedDevice
        private object selectedDevice;
        public object SelectedDevice
        {
            get { return selectedDevice; }
            set
            {
                if (ReferenceEquals(selectedDevice, value))
                    return;
                selectedDevice = value;
                RaisePropertyChanged(nameof(SelectedDevice));
            }
        }
        #endregion // SelectedDevice

        #region Theme
        private bool isDarkTheme;
        public bool IsDarkTheme
        {
            get { return isDarkTheme; }
            set
            {
                if (isDarkTheme == value)
                    return;
                isDarkTheme = value;
                ApplyTheme();
                if (ConfigHolder.Configuration != null)
                    ConfigHolder.Configuration.IsDarkTheme = value;
                RaisePropertyChanged(nameof(IsDarkTheme));
                RaisePropertyChanged(nameof(ThemeToggleLabel));
            }
        }

        public string ThemeToggleLabel
        {
            get { return isDarkTheme ? "Light theme" : "Dark theme"; }
        }

        public RelayCommand ToggleThemeCommand { get; private set; }

        private void executeToggleTheme(object o)
        {
            IsDarkTheme = !IsDarkTheme;
        }

        private void ApplyTheme()
        {
            App app = Application.Current as App;
            if (app != null)
                app.ApplyTheme(isDarkTheme);
        }
        #endregion // Theme

        #region ConfigHolder PropertyChanged
        private void OnConfigHolderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConfigHolder.Configuration))
            {
                SelectedDevice = null;
                BuildDeviceGroups();
                if (ConfigHolder.Configuration != null && ConfigHolder.Configuration.IsDarkTheme != isDarkTheme)
                {
                    IsDarkTheme = ConfigHolder.Configuration.IsDarkTheme;
                }
            }
        }
        #endregion

        #region view
        private readonly Window view;
        #endregion // view
    }
}
