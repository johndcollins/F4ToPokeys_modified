using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;
using Pololu.UsbWrapper;
using Pololu.Usc;

namespace F4ToPokeys
{
    public class PololuMaestro : BindableObject, IDisposable
    {
        #region Construction
        public PololuMaestro()
        {
            RemovePololuMaestroCommand = new RelayCommand(executeRemovePololuMaestro);
            AddServoCommand = new RelayCommand(executeAddServo);
        }

        public void Dispose()
        {
            foreach (PololuMaestroServo servo in ServoList)
                servo.Dispose();

            Device = null;
        }
        #endregion

        #region SerialNumberList
        [XmlIgnore]
        public List<string> SerialNumberList
        {
            get
            {
                if (serialNumberList == null)
                {
                    serialNumberList = PololuMaestroEnumerator.Singleton.AvailablePololuMaestroList
                        .Select(availablePololuMaestro => availablePololuMaestro.serialNumber)
                        .Union(SerialNumberAsEnumerable())
                        .OrderBy(serialNumber => serialNumber)
                        .ToList();
                }
                return serialNumberList;
            }
        }
        private List<string> serialNumberList;
        #endregion

        #region SerialNumber
        public string SerialNumber
        {
            get { return serialNumber; }
            set
            {
                serialNumber = value;
                RaisePropertyChanged("SerialNumber");

                updateStatus();
                updateServoStatus();
            }
        }
        private string serialNumber;

        private IEnumerable<string> SerialNumberAsEnumerable()
        {
            if (!string.IsNullOrWhiteSpace(SerialNumber))
                yield return SerialNumber;
        }
        #endregion

        #region RemovePololuMaestroCommand
        [XmlIgnore]
        public RelayCommand RemovePololuMaestroCommand { get; private set; }

        private void executeRemovePololuMaestro(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemovePololuMaestroText, SerialNumber),
                Translations.Main.RemovePololuMaestroCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.PololuMaestroList.Remove(this);
            Dispose();
        }
        #endregion

        #region ServoList
        public ObservableCollection<PololuMaestroServo> ServoList
        {
            get { return servoList; }
            set
            {
                servoList = value;
                RaisePropertyChanged("ServoList");
            }
        }
        private ObservableCollection<PololuMaestroServo> servoList = new ObservableCollection<PololuMaestroServo>();
        #endregion

        #region AddServoCommand
        [XmlIgnore]
        public RelayCommand AddServoCommand { get; private set; }

        private void executeAddServo(object o)
        {
            PololuMaestroServo servo = new PololuMaestroServo();
            servo.setOwner(this);
            ServoList.Add(servo);
        }
        #endregion

        #region Error
        [XmlIgnore]
        public string Error
        {
            get { return error; }
            set
            {
                if (error == value)
                    return;
                error = value;
                RaisePropertyChanged("Error");
            }
        }
        private string error;
        #endregion

        #region owner
        public void setOwner(Configuration configuration)
        {
            owner = configuration;
            updateStatus();

            foreach (PololuMaestroServo servo in ServoList)
                servo.setOwner(this);
        }

        private Configuration owner;
        #endregion

        #region Device
        [XmlIgnore]
        public Usc Device
        {
            get { return device; }
            set
            {
                if (device == value)
                    return;
                if (device != null)
                {
                    try
                    {
                        device.Dispose();
                    }
                    catch
                    {
                    }
                }
                device = value;
            }
        }
        private Usc device;
        #endregion

        #region updateStatus
        private void updateStatus()
        {
            if (owner == null)
                return;

            Device = null;

            if (string.IsNullOrWhiteSpace(SerialNumber))
            {
                Error = null;
            }
            else
            {
                DeviceListItem availablePololuMaestro = PololuMaestroEnumerator.Singleton.AvailablePololuMaestroList.FirstOrDefault(item => item.serialNumber == SerialNumber);
                if (availablePololuMaestro == null)
                {
                    Error = Translations.Main.PololuMaestroNotFoundError;
                }
                else
                {
                    try
                    {
                        Device = new Usc(availablePololuMaestro);
                        Error = null;
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                    }
                }
            }
        }

        private void updateServoStatus()
        {
            if (owner == null)
                return;

            foreach (PololuMaestroServo servo in ServoList)
                servo.updateStatus();
        }
        #endregion
    }
}
