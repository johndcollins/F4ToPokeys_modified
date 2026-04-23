using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;

namespace F4ToPokeys
{
    public class ConfigurationViewModel : BindableObject
    {
        #region Construction/Destruction
        public ConfigurationViewModel(Window view)
        {
            this.view = view;
            SaveCommand = new RelayCommand(executeSave);
            SaveAsCommand = new RelayCommand(executeSaveAs);
            OpenCommand = new RelayCommand(executeOpen);
            NewCommand = new RelayCommand(executeNew);
            MinimizeCommand = new RelayCommand(executeMinimize);
            QuitCommand = new RelayCommand(executeQuit);
            ToggleThemeCommand = new RelayCommand(executeToggleTheme);

            // Bubble up ConfigHolder.CurrentFilePath changes to Title so the title bar
            // reflects which file is open, and persist the path to preferences so the app
            // reopens it on next launch.
            ConfigHolder.Singleton.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConfigHolder.CurrentFilePath))
                {
                    RaisePropertyChanged(nameof(Title));
                    PersistUserPreferences();
                }
            };

            // Status dot reflects FalconConnector live state
            FalconConnector.Singleton.FalconStarted += OnFalconStarted;
            FalconConnector.Singleton.FalconStopped += OnFalconStopped;

            // Rebuild the tree whenever Configuration is replaced (Load)
            ConfigHolder.Singleton.PropertyChanged += OnConfigHolderPropertyChanged;

            // Persist sampling interval to preferences.xml when the user edits it.
            HookConfigurationPropertyChanged(ConfigHolder.Configuration);

            isDarkTheme = ConfigHolder.Configuration?.IsDarkTheme ?? true;

            // StartMinimized is pure user preference — read directly from preferences.xml.
            startMinimized = UserPreferences.Load().StartMinimized;

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

                string path = ConfigHolder.Singleton.CurrentFilePath;
                if (!string.IsNullOrEmpty(path))
                {
                    string fileName = Path.GetFileName(path);
                    if (!string.IsNullOrEmpty(fileName))
                        title += " — " + fileName;
                }

                return title;
            }
        }
        #endregion

        #region ConfigHolder
        public ConfigHolder ConfigHolder { get { return ConfigHolder.Singleton; } }
        #endregion // ConfigHolder

        #region SaveCommand (quick-save to the currently-open file)
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
        #endregion

        #region SaveAsCommand (prompts for a path, then saves + switches current file)
        public RelayCommand SaveAsCommand { get; private set; }

        private void executeSaveAs(object o)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "F4ToPokeys configuration (*.xml)|*.xml|All files (*.*)|*.*",
                DefaultExt = ".xml",
                FileName = Path.GetFileName(ConfigHolder.Singleton.CurrentFilePath ?? "F4ToPokeys.xml"),
                InitialDirectory = GetInitialDirectory(),
                OverwritePrompt = true
            };
            if (dlg.ShowDialog(view) != true)
                return;

            try
            {
                ConfigHolder.Singleton.SaveTo(dlg.FileName);
            }
            catch (Exception e)
            {
                MessageBox.Show(view, e.Message, Translations.Main.ConfigSaveErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region OpenCommand (prompts for a path, then loads + switches current file)
        public RelayCommand OpenCommand { get; private set; }

        private void executeOpen(object o)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "F4ToPokeys configuration (*.xml)|*.xml|All files (*.*)|*.*",
                DefaultExt = ".xml",
                InitialDirectory = GetInitialDirectory(),
                CheckFileExists = true
            };
            if (dlg.ShowDialog(view) != true)
                return;

            try
            {
                ConfigHolder.Singleton.LoadFrom(dlg.FileName);
            }
            catch (Exception e)
            {
                MessageBox.Show(view, e.Message, Translations.Main.ConfigLoadErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region NewCommand (reset to empty config, current file = default path)
        public RelayCommand NewCommand { get; private set; }

        private void executeNew(object o)
        {
            MessageBoxResult result = MessageBox.Show(view,
                "Start a new empty configuration? Unsaved changes to the current one will be lost.",
                "New configuration",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
                return;

            Configuration fresh = new Configuration();
            fresh.setOwner();
            ConfigHolder.Singleton.Configuration = fresh;
            ConfigHolder.Singleton.ResetCurrentFilePathToDefault();
        }
        #endregion

        #region MinimizeCommand (closes the dialog; app keeps running in tray)
        public RelayCommand MinimizeCommand { get; private set; }

        private void executeMinimize(object o)
        {
            view.Close();
        }
        #endregion

        #region QuitCommand (saves the current config, then fully shuts down the application)
        public RelayCommand QuitCommand { get; private set; }

        private void executeQuit(object o)
        {
            // Save explicitly here rather than relying on Window_Unloaded firing during
            // shutdown — that race could have Save() running while the app's resource
            // tree is being torn down. Explicit Save first, then Shutdown.
            try
            {
                ConfigHolder.Save();
            }
            catch (Exception e)
            {
                MessageBox.Show(view, e.Message, Translations.Main.ConfigSaveErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                // If the save failed, still quit (user chose Quit). Don't strand them.
            }

            Application.Current?.Shutdown();
        }
        #endregion

        private static string GetInitialDirectory()
        {
            string currentPath = ConfigHolder.Singleton.CurrentFilePath;
            if (!string.IsNullOrEmpty(currentPath))
            {
                string dir = Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return dir;
            }
            return ConfigHolder.AppDataPath;
        }

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
                PersistUserPreferences();
                RaisePropertyChanged(nameof(IsDarkTheme));
                RaisePropertyChanged(nameof(ThemeToggleLabel));
            }
        }

        // Persist the user's local preferences immediately so theme/sampling/last-opened
        // changes survive a crash without waiting for orderly shutdown.
        private void PersistUserPreferences()
        {
            UserPreferences prefs = new UserPreferences
            {
                IsDarkTheme = isDarkTheme,
                ReadFalconDataTimerIntervalMS = FalconConnector.Singleton.ReadFalconDataTimerInterval.TotalMilliseconds,
                LastOpenedConfigPath = ConfigHolder.Singleton.CurrentFilePath,
                StartMinimized = startMinimized
            };
            prefs.Save();
        }

        #region StartMinimized
        private bool startMinimized;
        public bool StartMinimized
        {
            get { return startMinimized; }
            set
            {
                if (startMinimized == value)
                    return;
                startMinimized = value;
                PersistUserPreferences();
                RaisePropertyChanged(nameof(StartMinimized));
            }
        }
        #endregion

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
        private Configuration hookedConfiguration;

        private void HookConfigurationPropertyChanged(Configuration config)
        {
            if (hookedConfiguration != null)
                hookedConfiguration.PropertyChanged -= OnConfigurationPropertyChanged;
            hookedConfiguration = config;
            if (hookedConfiguration != null)
                hookedConfiguration.PropertyChanged += OnConfigurationPropertyChanged;
        }

        private void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Configuration.ReadFalconDataTimerIntervalMS))
                PersistUserPreferences();
        }

        private void OnConfigHolderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConfigHolder.Configuration))
            {
                SelectedDevice = null;
                BuildDeviceGroups();
                HookConfigurationPropertyChanged(ConfigHolder.Configuration);
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
