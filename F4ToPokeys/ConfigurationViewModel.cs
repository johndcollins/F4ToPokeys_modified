using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

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

        #region FalconLightList
        public List<FalconLight> FalconLightList
        {
            get { return FalconConnector.Singleton.LightList; }
        }
        #endregion // FalconLightList

        #region FalconGaugeList
        public List<FalconGauge> FalconGaugeList
        {
            get { return FalconConnector.Singleton.GaugeList; }
        }
        #endregion

        #region view
        private readonly Window view;
        #endregion // view
    }
}
