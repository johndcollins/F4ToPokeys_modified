using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class ConfigHolder : BindableObject
    {
        #region Singleton
        public static ConfigHolder Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new ConfigHolder();
                return singleton;
            }
        }
        private static ConfigHolder singleton;
        #endregion // Singleton

        #region Configuration
        public Configuration Configuration
        {
            get { return configuration; }
            set
            {
                if (configuration == value)
                    return;
                if (configuration != null)
                    configuration.Dispose();
                configuration = value;
                RaisePropertyChanged("Configuration");
            }
        }
        private Configuration configuration = new Configuration();
        #endregion // Configuration

        #region XML Serialization

        // Store config file under User's AppData/Local path to avoid having to run F4ToPokeys as Administrator
        public static string AppDataPath = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "F4ToPokeys");
        private static string configFileName = Path.Combine(AppDataPath, "F4ToPokeys.xml");

        public void Save()
        {
            Configuration.FormatVersion = Configuration.CurrentFormatVersion.ToString();

            // Create F4ToPokeys directory under Users AppData/Local path if it doesn't exist.
            Directory.CreateDirectory(AppDataPath);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));

            // Config file now under User's AppData/Local/F4ToPokeys
            using (Stream file = File.Create(configFileName))
            {
                xmlSerializer.Serialize(file, Configuration);
            }
        }

        public void Load()
        {
            if (!File.Exists(configFileName))
            {
                TryToUpdateFromFormatV1_0ToV1_1();

                if (!File.Exists(configFileName))
                    return;
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));

            // Config file now under User's AppData/Local/F4ToPokeys
            using (Stream file = File.OpenRead(configFileName))
            {
                Configuration newConfiguration = (Configuration)xmlSerializer.Deserialize(file);
                newConfiguration.setOwner();
                Configuration = newConfiguration;
            }
        }

        private void TryToUpdateFromFormatV1_0ToV1_1()
        {
            Version formatVersionV1_0 = new Version(1, 0);
            Version formatVersionV1_1 = new Version(1, 1);

            string oldConfigFileName = "F4ToPokeys.xml";

            if (!File.Exists(oldConfigFileName))
                return;

            XDocument oldConfiguration = XDocument.Load(oldConfigFileName);

            Version oldFormatVersion;
            if (!Version.TryParse((string)oldConfiguration.Root.Attribute("formatVersion"), out oldFormatVersion))
                oldFormatVersion = formatVersionV1_0;

            if (oldFormatVersion != formatVersionV1_0)
                return;

            oldConfiguration.Root.SetAttributeValue("formatVersion", formatVersionV1_1);

            foreach (XElement poKeysElement in oldConfiguration.Root.Descendants("PoKeys"))
            {
                byte? userId = (byte?)(uint?)poKeysElement.Element("UserId");
                if (userId.HasValue)
                {
                    AvailablePoKeys availablePoKeys = PoKeysEnumerator.Singleton.AvailablePoKeysList
                        .FirstOrDefault(ap => ap.PokeysUserId == userId);

                    if (availablePoKeys != null)
                        poKeysElement.SetElementValue("Serial", availablePoKeys.PokeysSerial);

                    poKeysElement.SetElementValue("UserId", null);
                }
            }

            Dictionary<string, string> renamedLightLabels = new Dictionary<string, string>();
            renamedLightLabels.Add("OXY LOW (Eyebrown)", "OXY LOW (R. Warning)");
            renamedLightLabels.Add("LEFlaps", "LE FLAPS");
            renamedLightLabels.Add("Fuel Low", "FUEL LOW");
            renamedLightLabels.Add("PRIORITY MODE", "PRIORITY");
            renamedLightLabels.Add("Bingo chaff", "BINGO CHAFF");
            renamedLightLabels.Add("Bingo flare", "BINGO FLARE");
            renamedLightLabels.Add("EPU On", "EPU ON");
            renamedLightLabels.Add("Gear handle", "GEAR HANDLE");
            renamedLightLabels.Add("Lef Fault", "LEF FAULT");
            renamedLightLabels.Add("Outer Marker", "OUTER MARKER");
            renamedLightLabels.Add("Middle Marker", "MIDDLE MARKER");
            renamedLightLabels.Add("Nose gear down", "NOSE GEAR DOWN");
            renamedLightLabels.Add("Left gear down", "LEFT GEAR DOWN");
            renamedLightLabels.Add("Right gear down", "RIGHT GEAR DOWN");
            renamedLightLabels.Add("SpeedBrake > 0%", "SPEEDBRAKE > 0%");
            renamedLightLabels.Add("SpeedBrake > 33%", "SPEEDBRAKE > 33%");
            renamedLightLabels.Add("SpeedBrake > 66%", "SPEEDBRAKE > 66%");
            renamedLightLabels.Add("FLCS BIT Magnetic Switch Off", "FLCS BIT DIY MAG SW RESET");
            renamedLightLabels.Add("JFS Magnetic Switch Off", "JFS DIY MAG SW RESET");
            renamedLightLabels.Add("Parking Brake Magnetic Switch Off", "PARK BRAKE DIY MAG SW RESET");
            renamedLightLabels.Add("Autopilot PITCH Magnetic Switch Off", "AUTOPILOT DIY MAG SW RESET");

            foreach (string oldLabel in renamedLightLabels.Keys)
            {
                foreach (XElement falconLightLabelElement in oldConfiguration.Root.Descendants("FalconLightLabel").Where(e => (string)e == oldLabel))
                    falconLightLabelElement.SetValue(renamedLightLabels[oldLabel]);
            }

            Dictionary<string, string> renamedGaugeLabels = new Dictionary<string, string>();
            renamedGaugeLabels.Add("NOZ Pos", "NOZ POS");
            renamedGaugeLabels.Add("NOZ Pos 2", "NOZ POS 2");
            renamedGaugeLabels.Add("SpeedBrake", "SPEEDBRAKE");
            renamedGaugeLabels.Add("Oil Pressure", "OIL PRESSURE");
            renamedGaugeLabels.Add("Oil Pressure 2", "OIL PRESSURE 2");
            renamedGaugeLabels.Add("Chaff Count", "CHAFF COUNT");
            renamedGaugeLabels.Add("Flare Count", "FLARE COUNT");
            renamedGaugeLabels.Add("Trim Pitch", "TRIM PITCH");
            renamedGaugeLabels.Add("Trim Roll", "TRIM ROLL");
            renamedGaugeLabels.Add("Trim Yaw", "TRIM YAW");
            renamedGaugeLabels.Add("Current Heading", "CURRENT HEADING");

            foreach (string oldLabel in renamedGaugeLabels.Keys)
            {
                foreach (XElement falconGaugeLabelElement in oldConfiguration.Root.Descendants("FalconGaugeLabel").Where(e => (string)e == oldLabel))
                    falconGaugeLabelElement.SetValue(renamedGaugeLabels[oldLabel]);
            }

            oldConfiguration.Save(configFileName);
        }

        #endregion // XML Serialization
    }

    public class Configuration : BindableObject, IDisposable
    {
        #region Construction/Destruction

        public Configuration()
        {
            AddPoKeysCommand = new RelayCommand(executeAddPoKeys);
            AddPololuMaestroCommand = new RelayCommand(executeAddPololuMaestro);
        }

        public void setOwner()
        {
            foreach (PoKeys poKeys in PoKeysList)
                poKeys.setOwner(this);

            foreach (PololuMaestro pololuMaestro in PololuMaestroList)
                pololuMaestro.setOwner(this);
        }

        public void Dispose()
        {
            foreach (PoKeys poKeys in PoKeysList)
                poKeys.Dispose();

            foreach (PololuMaestro pololuMaestro in PololuMaestroList)
                pololuMaestro.Dispose();
        }

        #endregion // Construction/Destruction

        #region FormatVersion
        [XmlIgnore]
        public static Version CurrentFormatVersion { get; } = new Version(1, 1);

        [XmlAttribute("formatVersion")]
        public string FormatVersion { get; set; }
        #endregion

        #region ReadFalconDataTimerIntervalMS
        public double ReadFalconDataTimerIntervalMS
        {
            get { return FalconConnector.Singleton.ReadFalconDataTimerInterval.TotalMilliseconds; }
            set { FalconConnector.Singleton.ReadFalconDataTimerInterval = TimeSpan.FromMilliseconds(value); }
        }
        #endregion

        #region PoKeysList
        public ObservableCollection<PoKeys> PoKeysList
        {
            get { return poKeysList; }
            set
            {
                poKeysList = value;
                RaisePropertyChanged("PoKeysList");
            }
        }
        private ObservableCollection<PoKeys> poKeysList = new ObservableCollection<PoKeys>();
        #endregion // PoKeysList

        #region AddPoKeysCommand
        [XmlIgnore]
        public RelayCommand AddPoKeysCommand { get; private set; }

        private void executeAddPoKeys(object o)
        {
            PoKeys poKeys = new PoKeys();
            poKeys.setOwner(this);
            PoKeysList.Add(poKeys);
        }
        #endregion // AddPoKeysCommand

        #region PololuMaestroList
        public ObservableCollection<PololuMaestro> PololuMaestroList
        {
            get { return pololuMaestroList; }
            set
            {
                pololuMaestroList = value;
                RaisePropertyChanged("PololuMaestroList");
            }
        }
        private ObservableCollection<PololuMaestro> pololuMaestroList = new ObservableCollection<PololuMaestro>();
        #endregion // PoKeysList

        #region AddPololuMaestroCommand
        [XmlIgnore]
        public RelayCommand AddPololuMaestroCommand { get; private set; }

        private void executeAddPololuMaestro(object o)
        {
            PololuMaestro pololuMaestro = new PololuMaestro();
            pololuMaestro.setOwner(this);
            PololuMaestroList.Add(pololuMaestro);
        }
        #endregion // AddPoKeysCommand
    }
}
