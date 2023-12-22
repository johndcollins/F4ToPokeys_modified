using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Security.Permissions;
using System.Windows;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class ArduinoGauge : BindableObject, IDisposable
    {
        #region Construction/Destruction
        public ArduinoGauge()
        {
            RemoveArduinoGaugeDriverCommand = new RelayCommand(ExecuteRemoveArduinoGaugeDriver);
            AddStepperMotorCommand = new RelayCommand(ExecuteAddStepperMotor);
        }

        public void Dispose()
        {
            foreach (ArduinoGaugeStepperMotor stepper in StepperMotorList)
                stepper.Dispose();

            // TODO: Dispose of device?
        }
        #endregion

        #region Device
        private ArduinoGaugeDriver device;
        [XmlIgnore]
        public ArduinoGaugeDriver Device
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
        #endregion

        #region StepperMotorList
        private ObservableCollection<ArduinoGaugeStepperMotor> stepperMotorList = new ObservableCollection<ArduinoGaugeStepperMotor>();
        public ObservableCollection<ArduinoGaugeStepperMotor> StepperMotorList
        {
            get { return stepperMotorList; }
            set
            {
                stepperMotorList = value;
                RaisePropertyChanged("StepperMotorList");
            }
        }
        #endregion

        #region Owner
        private Configuration owner;
        public void SetOwner(Configuration config)
        {
            owner = config;

            UpdateStatus();

            foreach (ArduinoGaugeStepperMotor stepper in StepperMotorList)
                stepper.SetOwner(this);
        }
        #endregion

        #region SerialNumber
        private string serialNumber;
        public string SerialNumber
        {
            get { return serialNumber; }
            set
            {
                serialNumber = value;
                RaisePropertyChanged("SerialNumber");

                UpdateStatus();
                UpdateStepperMotorStatus();
            }
        }

        private IEnumerable<string> SerialNumberAsEnumerable()
        {
            if (!string.IsNullOrWhiteSpace(SerialNumber))
                yield return SerialNumber;
        }

        private List<string> serialNumberList;
        [XmlIgnore]
        public List<string> SerialNumberList
        {
            get
            {
                if (serialNumberList == null)
                {
                    serialNumberList = ArduinoGaugeEnumerator.Singleton.AvailableArduinoGaugeDeviceList
                        .Select(driver => driver.SerialNumber)
                        .Union(SerialNumberAsEnumerable())
                        .OrderBy(serialNumber => serialNumber)
                        .ToList();
                }
                return serialNumberList;
            }
        }
        #endregion

        #region Error
        private string error;
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
        #endregion

        #region Update
        private void UpdateStatus()
        {
            if (owner == null)
                return;

            Device = null;

            if (string.IsNullOrWhiteSpace(SerialNumber))
                Error = null;
            else
            {
                ArduinoGaugeDevice availableArduinoGaugeDriver = ArduinoGaugeEnumerator.Singleton.AvailableArduinoGaugeDeviceList.FirstOrDefault(XmlArrayItemAttribute => XmlArrayItemAttribute.SerialNumber == SerialNumber);
                if (availableArduinoGaugeDriver == null)
                {
                    Error = Translations.Main.ArduinoGaugeDriverNotFoundError;
                }
                else
                {
                    try
                    {
                        Device = new ArduinoGaugeDriver(availableArduinoGaugeDriver);
                        Error = null;
                    }
                    catch (Exception ex) 
                    {
                        Error = ex.Message;
                        throw;
                    }
                }
            }
        }

        private void UpdateStepperMotorStatus()
        {
            if (owner == null)
                return;

            foreach (ArduinoGaugeStepperMotor stepper in StepperMotorList)
                stepper.UpdateStatus();
        }
        #endregion

        #region Commands
        [XmlIgnore]
        public RelayCommand RemoveArduinoGaugeDriverCommand { get; private set; }
        private void ExecuteRemoveArduinoGaugeDriver(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemoveArduinoGaugeDriverText, SerialNumber),
                Translations.Main.RemoveArduinoGaugeDriverCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.ArduinoGaugeList.Remove(this);
            Dispose();
        }

        [XmlIgnore]
        public RelayCommand AddStepperMotorCommand { get; private set; }
        private void ExecuteAddStepperMotor(object o)
        {
            ArduinoGaugeStepperMotor stepperMotor = new ArduinoGaugeStepperMotor();
            stepperMotor.SetOwner(this);
            StepperMotorList.Add(stepperMotor);
        }
        #endregion
    }
}