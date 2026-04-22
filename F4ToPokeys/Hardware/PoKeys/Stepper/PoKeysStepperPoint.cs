namespace F4ToPokeys
{
    public class PoKeysStepperPoint : BindableObject
    {
        #region FalconValue
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
        private float falconValue;
        #endregion

        #region StepperMotorValue
        public int StepperValue
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
        private int stepperValue;
        #endregion
    }
}
