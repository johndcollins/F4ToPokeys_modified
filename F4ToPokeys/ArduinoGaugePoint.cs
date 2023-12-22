namespace F4ToPokeys
{
    public class ArduinoGaugePoint : BindableObject
    {
        private float falconValue;
        public float FalconValue
        {
            get { return falconValue; }
            set
            {
                if (falconValue == value)
                    return;
                falconValue = value;
                RaisePropertyChanged("FalconValue");
            }
        }

        private ushort stepperValue;
        public ushort StepperValue
        {
            get { return stepperValue; }
            set
            {
                if (stepperValue == value)
                    return;
                stepperValue = value;
                RaisePropertyChanged("StepperValue");
            }
        }
    }
}