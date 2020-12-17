using PoKeysDevice_DLL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class PoKeysStepper : BindableObject, IDisposable
    {
        #region Constructor
        public PoKeysStepper()
        {
            RemoveStepperCommand = new RelayCommand(executeRemoveStepper);
            AddAdditionalPointCommand = new RelayCommand(executeAddAdditionalPoint);
            RemoveAdditionalPointCommand = new RelayCommand(executeRemoveAdditionalPoint, canExecuteRemoveAdditionalPoint);
            HomeStepperCommand = new RelayCommand(executeHomeStepper);

            HomePinSwitchList = new List<int>();
            for (int i = 0; i < 54; i++)
                HomePinSwitchList.Add(i);
            HomePinSwitchList.Add(55);

            minPoint = DefaultMinPoint();
            maxPoint = DefaultMaxPoint();
            AdditionalPointList = new ObservableCollection<PoKeysStepperPoint>();

            HasHomeSwitch = true;
            SoftLimitEnabled = true;
            SoftLimitMaximum = 3820;
            PinHomeSwitch = 0;

            HomeInverted = false;
        }

        public void Dispose()
        {
            if (falconGauge != null)
                falconGauge.FalconGaugeChanged -= OnFalconGaugeChanged;
        }
        #endregion

        #region Public Properties
        private bool continuousRotation;
        public bool ContinuousRotation
        {
            get { return continuousRotation; }
            set
            {
                continuousRotation = value;
                RaisePropertyChanged("ContinuousRotation");
            }
        }

        private bool softLimitEnabled;
        public bool SoftLimitEnabled
        {
            get { return softLimitEnabled; }
            set
            {
                softLimitEnabled = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("SoftLimitEnabled");
            }
        }

        private bool inverted;
        public bool Inverted
        {
            get { return inverted; }
            set
            {
                inverted = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("Inverted");
            }
        }

        private bool homeInverted = false;
        public bool HomeInverted
        {
            get { return homeInverted; }
            set
            {
                homeInverted = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("HomeInverted");
            }
        }
        private float maxSpeed = 10;
        public float MaxSpeed
        {
            get { return maxSpeed; }
            set
            {
                maxSpeed = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("MaxSpeed");
            }
        }
        private float maxAcceleration = 0.5f;
        public float MaxAcceleration
        {
            get { return maxAcceleration; }
            set
            {
                maxAcceleration = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("MaxAcceleration");
            }
        }
        private float maxDecceleration = 0.5f;
        public float MaxDecceleration
        {
            get { return maxDecceleration; }
            set
            {
                maxDecceleration = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("MaxDecceleration");
            }
        }
        private int softLimitMaximum = 3780;
        public int SoftLimitMaximum
        {
            get { return softLimitMaximum; }
            set
            {
                softLimitMaximum = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("SoftLimitMaximum");
            }
        }
        private int softLimitMinimum = 0;
        public int SoftLimitMinimum
        {
            get { return softLimitMinimum; }
            set
            {
                softLimitMinimum = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("SoftLimitMinimum");
            }
        }
        private int homingSpeed = 50;
        public int HomingSpeed
        {
            get { return homingSpeed; }
            set
            {
                homingSpeed = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("HomingSpeed");
            }
        }
        private int homingReturnSpeed = 20;
        public int HomingReturnSpeed
        {
            get { return homingReturnSpeed; }
            set
            {
                homingReturnSpeed = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("HomingReturnSpeed");
            }
        }
        private int pinHomeSwitch = 0;
        public int PinHomeSwitch
        {
            get { return pinHomeSwitch; }
            set
            {
                Error = null;

                if (HasHomeSwitch && (!HomePinSwitchList.Contains(value)))
                    Error = Translations.Main.StepperPinHomeSwitchOutOfRange;

                pinHomeSwitch = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("PinHomeSwitch");
            }
        }
        private bool hasHomeSwitch = false;
        public bool HasHomeSwitch
        {
            get { return hasHomeSwitch; }
            set
            {
                hasHomeSwitch = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("HasHomeSwitch");
            }
        }
        #endregion

        #region StepperIdList
        [XmlIgnore]
        public List<int> StepperIdList => new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
        #endregion

        #region StepperId
        public int? StepperId
        {
            get { return stepperId; }
            set
            {
                if (stepperId == value)
                    return;

                stepperId = value;
                if (owner != null)
                    owner.SetAxisParameters(this);
                RaisePropertyChanged("StepperId");

                ResetStepperIdErrors();
            }
        }
        private int? stepperId;

        private void ResetStepperIdErrors()
        {
            if (owner != null)
            {
                foreach (PoKeysStepper stepper in owner.StepperList)
                {
                    stepper.StepperIdError = null;
                }

                foreach (PoKeysStepper stepper in owner.StepperList)
                {
                    if (string.IsNullOrEmpty(stepper.StepperIdError))
                    {
                        foreach (PoKeysStepper stepper2 in owner.StepperList)
                        {
                            if ((stepper != stepper2) && (stepper.StepperId == stepper2.StepperId) && (string.IsNullOrEmpty(stepper.StepperIdError)))
                            {
                                StepperIdError = string.Format(Translations.Main.StepperIdIsAlreadyUsed, StepperId);
                            }
                            else
                                stepper.StepperIdError = null;
                        }
                    }
                }
            }
        }
        #endregion

        #region HomePinList
        [XmlIgnore]
        public List<int> HomePinSwitchList { get; private set; }
        #endregion

        #region Full Turn Value
        public int FullTurnValue
        {
            get { return fullTurnValue; }
            set
            {
                if (value <= 0)
                    return;

                if (fullTurnValue == value)
                    return;

                fullTurnValue = value;
                RaisePropertyChanged("FullTurnValue");
            }
        }
        private int fullTurnValue = 360;
        #endregion

        #region RemoveStepperCommand
        [XmlIgnore]
        public RelayCommand RemoveStepperCommand { get; private set; }

        private void executeRemoveStepper(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemoveStepperText, StepperId),
                Translations.Main.RemoveStepperCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.StepperList.Remove(this);
            owner.StepperRemoved();
            Dispose();
        }
        #endregion

        #region HomeStepperCommand
        [XmlIgnore]
        public RelayCommand HomeStepperCommand { get; private set; }

        private void executeHomeStepper(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.StepperHomeText, StepperId),
                Translations.Main.StepperHomeCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            owner.StartHomingSingle(this);
        }
        #endregion

        #region FalconGauge
        [XmlIgnore]
        public FalconGauge FalconGauge
        {
            get { return falconGauge; }
            set
            {
                if (falconGauge == value)
                    return;
                if (falconGauge != null)
                    falconGauge.FalconGaugeChanged -= OnFalconGaugeChanged;
                falconGauge = value;
                if (falconGauge != null)
                    falconGauge.FalconGaugeChanged += OnFalconGaugeChanged;
                RaisePropertyChanged("FalconGauge");

                if (falconGauge == null)
                    FalconGaugeLabel = null;
                else
                    FalconGaugeLabel = falconGauge.Label;

                resetFalconValue();

                if (falconGauge != null)
                {
                    MinPoint.FalconValue = falconGauge.MinValue;
                    MaxPoint.FalconValue = falconGauge.MaxValue;
                }
                else
                {
                    MinPoint.FalconValue = DefaultFalconMinValue;
                    MaxPoint.FalconValue = DefaultFalconMaxValue;
                }

                AdditionalPointList.Clear();
            }
        }
        private FalconGauge falconGauge;
        #endregion

        #region FalconGaugeLabel
        public string FalconGaugeLabel
        {
            get { return falconGaugeLabel; }
            set
            {
                if (falconGaugeLabel == value)
                    return;
                falconGaugeLabel = value;
                RaisePropertyChanged("FalconGaugeLabel");

                if (string.IsNullOrEmpty(falconGaugeLabel))
                    FalconGauge = null;
                else
                    FalconGauge = FalconConnector.Singleton.GaugeList.FirstOrDefault(item => item.Label == falconGaugeLabel);
            }
        }
        private string falconGaugeLabel;
        #endregion

        #region Error
        [XmlIgnore]
        public string StepperIdError
        {
            get { return stepperIdError; }
            set
            {
                if (stepperIdError == value)
                    return;
                stepperIdError = value;
                RaisePropertyChanged("StepperIdError");
            }
        }
        private string stepperIdError;

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

        #region FalconValue
        [XmlIgnore]
        public float? FalconValue
        {
            get { return falconValue; }
            set
            {
                if (falconValue == value)
                    return;
                falconValue = value;
                RaisePropertyChanged("FalconValue");

                StepperPosition = FalconValueToStepperValue(falconValue);
            }
        }
        private float? falconValue;
        #endregion

        #region MinPoint
        public PoKeysStepperPoint MinPoint
        {
            get { return minPoint; }
            set
            {
                if (minPoint == value)
                    return;
                if (minPoint != null)
                    minPoint.PropertyChanged -= OnPointChanged;
                minPoint = value;
                if (minPoint != null)
                    minPoint.PropertyChanged += OnPointChanged;
                RaisePropertyChanged("MinPoint");
                RaisePropertyChanged("Points");
                OnPointChanged(this, null);
            }
        }

        private PoKeysStepperPoint minPoint;
        public const float DefaultFalconMinValue = 0.0F;

        private PoKeysStepperPoint DefaultMinPoint()
        {
            PoKeysStepperPoint point = new PoKeysStepperPoint() { FalconValue = DefaultFalconMinValue, StepperValue = 0 };
            point.PropertyChanged += OnPointChanged;
            return point;
        }
        #endregion

        #region MaxPoint
        public PoKeysStepperPoint MaxPoint
        {
            get { return maxPoint; }
            set
            {
                if (maxPoint == value)
                    return;
                if (maxPoint != null)
                    maxPoint.PropertyChanged -= OnPointChanged;
                maxPoint = value;
                if (maxPoint != null)
                    maxPoint.PropertyChanged += OnPointChanged;
                RaisePropertyChanged("MaxPoint");
                RaisePropertyChanged("Points");
                OnPointChanged(this, null);
            }
        }

        private PoKeysStepperPoint maxPoint = new PoKeysStepperPoint() { FalconValue = DefaultFalconMaxValue, StepperValue = 4320 };
        public const float DefaultFalconMaxValue = 1.0F;

        private PoKeysStepperPoint DefaultMaxPoint()
        {
            float falconMaxValue = DefaultFalconMaxValue;
            if (falconGauge != null)
                falconMaxValue = falconGauge.MaxValue;

            PoKeysStepperPoint point = new PoKeysStepperPoint() { FalconValue = falconMaxValue, StepperValue = 4320 };
            point.PropertyChanged += OnPointChanged;
            return point;
        }
        #endregion

        #region AdditionalPointList
        public ObservableCollection<PoKeysStepperPoint> AdditionalPointList
        {
            get { return additionalPointList; }
            set
            {
                if (additionalPointList != null)
                {
                    additionalPointList.CollectionChanged -= OnAdditionalPointListChanged;
                    foreach (PoKeysStepperPoint additionalPoint in additionalPointList)
                        additionalPoint.PropertyChanged -= OnPointChanged;
                }

                additionalPointList = value;

                if (additionalPointList != null)
                {
                    additionalPointList.CollectionChanged += OnAdditionalPointListChanged;
                    foreach (PoKeysStepperPoint additionalPoint in additionalPointList)
                        additionalPoint.PropertyChanged += OnPointChanged;
                }

                RaisePropertyChanged("AdditionalPointList");
                RaisePropertyChanged("Points");
                OnPointChanged(this, null);
            }
        }
        private ObservableCollection<PoKeysStepperPoint> additionalPointList;

        private void OnAdditionalPointListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (PoKeysStepperPoint additionalPoint in e.NewItems.Cast<PoKeysStepperPoint>())
                        additionalPoint.PropertyChanged += OnPointChanged;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (PoKeysStepperPoint additionalPoint in e.OldItems.Cast<PoKeysStepperPoint>())
                        additionalPoint.PropertyChanged -= OnPointChanged;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (PoKeysStepperPoint additionalPoint in e.OldItems.Cast<PoKeysStepperPoint>())
                        additionalPoint.PropertyChanged -= OnPointChanged;
                    foreach (PoKeysStepperPoint additionalPoint in e.NewItems.Cast<PoKeysStepperPoint>())
                        additionalPoint.PropertyChanged += OnPointChanged;
                    break;
            }

            RaisePropertyChanged("Points");
            OnPointChanged(sender, e);
        }
        #endregion

        #region AddAdditionalPointCommand
        [XmlIgnore]
        public RelayCommand AddAdditionalPointCommand { get; private set; }

        private void executeAddAdditionalPoint(object o)
        {
            PoKeysStepperPoint previousPoint = AdditionalPointList.LastOrDefault() ?? MinPoint;
            PoKeysStepperPoint nextPoint = MaxPoint;
            PoKeysStepperPoint additionalPoint = new PoKeysStepperPoint()
            {
                FalconValue = (previousPoint.FalconValue + nextPoint.FalconValue) / 2,
                StepperValue = ((previousPoint.StepperValue + nextPoint.StepperValue) / 2)
            };
            AdditionalPointList.Add(additionalPoint);
        }
        #endregion

        #region RemoveAdditionalPointCommand
        [XmlIgnore]
        public RelayCommand RemoveAdditionalPointCommand { get; private set; }

        private void executeRemoveAdditionalPoint(object o)
        {
            PoKeysStepperPoint additionalPoint = (PoKeysStepperPoint)o;
            AdditionalPointList.Remove(additionalPoint);
        }

        private bool canExecuteRemoveAdditionalPoint(object o)
        {
            PoKeysStepperPoint additionalPoint = (PoKeysStepperPoint)o;
            return AdditionalPointList.Contains(additionalPoint);
        }
        #endregion

        #region OnPointChanged
        private void OnPointChanged(object sender, EventArgs e)
        {
            UpdateStepperPositionRange();
            StepperPosition = FalconValueToStepperValue(FalconValue);
        }
        #endregion

        #region Points
        public IEnumerable<PoKeysStepperPoint> Points
        {
            get
            {
                yield return MinPoint;

                foreach (PoKeysStepperPoint additionalPoint in AdditionalPointList)
                    yield return additionalPoint;

                yield return MaxPoint;
            }
        }
        #endregion

        #region StepperPosition Range
        [XmlIgnore]
        public int MinStepperValue { get; set; }

        [XmlIgnore]
        public int MaxStepperValue { get; set; }

        private void UpdateStepperPositionRange()
        {
            MinStepperValue = MinPoint.StepperValue;
            MaxStepperValue = MaxPoint.StepperValue;

            foreach (PoKeysStepperPoint point in Points)
            {
                if (point.StepperValue < MinStepperValue)
                    MinStepperValue = point.StepperValue;

                if (point.StepperValue > MaxStepperValue)
                    MaxStepperValue = point.StepperValue;
            }

            RaisePropertyChanged("MinStepperValue");
            RaisePropertyChanged("MaxStepperValue");
        }
        #endregion

        #region StepperPosition
        [XmlIgnore]
        public int StepperPosition
        {
            get
            { 
                return stepperPosition;
            }
            set
            {
                if (stepperPosition == value)
                    return;

                stepperPosition = value;
                RaisePropertyChanged("StepperPosition");

                updateStepperPosition();
            }
        }
        private int stepperPosition;
        #endregion

        #region owner
        public void setOwner(PoKeysStepperVID6066 poVID6066)
        {
            owner = poVID6066;
            OnPointChanged(this, null);

            // When loading the StepperId needs to be checked for errors
            ResetStepperIdErrors();
        }

        private PoKeysStepperVID6066 owner;
        #endregion

        #region updateStepperPosition
        private void updateStepperPosition()
        {
            if (string.IsNullOrEmpty(Error) && string.IsNullOrEmpty(StepperIdError) && owner != null && StepperId.HasValue && (StepperId.GetValueOrDefault() >= 0))
            {
                try
                {
                    owner.MoveStepper(StepperId.GetValueOrDefault() - 1, StepperPosition);
                }
                catch (Exception e)
                {
                    Error = e.Message;
                }
            }
        }

        #endregion

        #region OnFalconGaugeChanged
        private void OnFalconGaugeChanged(object sender, FalconGaugeChangedEventArgs e)
        {
            FalconValue = e.falconValue;
        }
        #endregion

        #region resetFalconValue
        private void resetFalconValue()
        {
            FalconValue = null;
        }
        #endregion

        #region FalconValueToStepperValue
        private int FalconValueToStepperValue(float? falconValue)
        {
            if (!falconValue.HasValue)
                return 0;

            if (!ContinuousRotation)
            {
                // Clamp value to [Min, Max]
                if (falconValue.Value <= MinPoint.FalconValue)
                    return MinPoint.StepperValue;

                if (falconValue.Value >= MaxPoint.FalconValue)
                    return MaxPoint.StepperValue;

                // Find the segment that contains falconValue
                PoKeysStepperPoint previousPoint = MinPoint;
                PoKeysStepperPoint nextPoint = null;

                foreach (PoKeysStepperPoint additionalPoint in AdditionalPointList)
                {
                    if (falconValue.Value >= additionalPoint.FalconValue)
                    {
                        previousPoint = additionalPoint;
                    }
                    else
                    {
                        nextPoint = additionalPoint;
                        break;
                    }
                }

                if (nextPoint == null)
                    nextPoint = MaxPoint;

                // Interpolate in segment
                float normalizedValue = 0.5F;
                if (previousPoint.FalconValue != nextPoint.FalconValue)
                    normalizedValue = (falconValue.Value - previousPoint.FalconValue) / (nextPoint.FalconValue - previousPoint.FalconValue);
                return (int)(normalizedValue * (nextPoint.StepperValue - previousPoint.StepperValue) + previousPoint.StepperValue);
            }
            else  // Working with Cont. Rotation stepper
            {
                int currentStepperPos = stepperPosition;
                //int currentStepperPos = owner.ReadStepper(StepperId.GetValueOrDefault() - 1);
                int stepsPerRotation = MaxPoint.StepperValue;
                
                int newStepPosition = (int)(falconValue.Value * (stepsPerRotation / (float)fullTurnValue));  // Convert desired position in degrees to steps

                //
                // Convert current step position (which could be quite large) to a Modulo 4320 step value.
                // This is step value equivilant to 0-360 (Modulo 360) in degrees.
                //
                int modulo4320CurrStepPosition = currentStepperPos % stepsPerRotation;
                int modulo4320NewStepPosition = newStepPosition % stepsPerRotation;

                if ((modulo4320CurrStepPosition < (stepsPerRotation / 2) + modulo4320CurrStepPosition) // Current value between 0 & 2160 steps (0 & 180 deg)?
                    && (modulo4320NewStepPosition > (stepsPerRotation / 2) + modulo4320CurrStepPosition)) // New position is between 2160 and 4320 (180 & 360 deg)?
                {
                    //
                    // Turning Left past 0 degrees. Do Negative Step Math.  (I need to figure this out)
                    //

                    // so if new pos is 4200 and current pos is 120 then 
                    // 4200 - 4310 = -120
                    // -120 - 120 = -240
                    modulo4320NewStepPosition = modulo4320NewStepPosition - stepsPerRotation; // find the negativ value from 0 back to new position
                    modulo4320NewStepPosition = modulo4320NewStepPosition - modulo4320CurrStepPosition; // add the minus current value to this

                    currentStepperPos += modulo4320NewStepPosition;
                }
                else
                {
                    // Otherwise do Positive Step Math
                
                    int difference = newStepPosition - modulo4320CurrStepPosition;
                    if (difference < -stepsPerRotation / 2)
                        difference += stepsPerRotation;
                    if (difference > stepsPerRotation)
                        difference -= stepsPerRotation;

                    currentStepperPos += difference;
                }
                return currentStepperPos;
            }
        }
        #endregion
    }
}