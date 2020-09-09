using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;

namespace F4ToPokeys
{
    public abstract class FalconLightConsumer : BindableObject, IDisposable
    {
        #region Construction/Destruction
        protected FalconLightConsumer()
        {
            SetOutputStateToOnCommand = new RelayCommand(executeSetOutputStateToOn);
            SetOutputStateToOffCommand = new RelayCommand(executeSetOutputStateToOff);
        }

        public void Dispose()
        {
            if (FalconLight != null)
                FalconLight.FalconLightChanged -= OnFalconLightChanged;
        }
        #endregion

        #region FalconLight
        [XmlIgnore]
        public FalconLight FalconLight
        {
            get { return falconLight; }
            set
            {
                if (falconLight == value)
                    return;
                if (falconLight != null)
                    falconLight.FalconLightChanged -= OnFalconLightChanged;
                falconLight = value;
                if (falconLight != null)
                    falconLight.FalconLightChanged += OnFalconLightChanged;
                RaisePropertyChanged("FalconLight");

                resetOutputState();

                if (falconLight == null)
                    FalconLightLabel = null;
                else
                    FalconLightLabel = falconLight.Label;
            }
        }
        private FalconLight falconLight;
        #endregion

        #region FalconLightLabel
        public string FalconLightLabel
        {
            get { return falconLightLabel; }
            set
            {
                if (falconLightLabel == value)
                    return;
                falconLightLabel = value;
                RaisePropertyChanged("FalconLightLabel");

                if (string.IsNullOrEmpty(falconLightLabel))
                    FalconLight = null;
                else
                    FalconLight = FalconConnector.Singleton.LightList.FirstOrDefault(item => item.Label == falconLightLabel);
            }
        }
        private string falconLightLabel;
        #endregion

        #region OnFalconLightChanged
        private void OnFalconLightChanged(object sender, FalconLightChangedEventArgs e)
        {
            if (e.newValue == true)
                OutputState = true;
            else
                OutputState = false;

            speakNewValue(e);
        }

        [Conditional("DEBUG")]
        private void speakNewValue(FalconLightChangedEventArgs e)
        {
            if (e.oldValue.HasValue && e.newValue.HasValue)
            {
                if (e.newValue == true)
                    DebugUtils.Speak(string.Format("{0} ON", FalconLight.Label));
                else
                    DebugUtils.Speak(string.Format("{0} OFF", FalconLight.Label));
            }
        }
        #endregion

        #region SetOutputStateToOnCommand
        [XmlIgnore]
        public RelayCommand SetOutputStateToOnCommand { get; private set; }

        public void executeSetOutputStateToOn(object o)
        {
            OutputState = true;
        }
        #endregion

        #region SetOutputStateToOffCommand
        [XmlIgnore]
        public RelayCommand SetOutputStateToOffCommand { get; private set; }

        public void executeSetOutputStateToOff(object o)
        {
            OutputState = false;
        }
        #endregion

        #region OutputState
        [XmlIgnore]
        public bool OutputState
        {
            get { return outputState; }
            set
            {
                outputState = value;
                RaisePropertyChanged("OutputState");

                writeOutputState();
            }
        }
        private bool outputState = false;

        protected abstract void writeOutputState();
        #endregion

        #region resetOutputState
        public void resetOutputState()
        {
            OutputState = false;
        }
        #endregion
    }
}
