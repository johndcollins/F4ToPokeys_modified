using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;
using Pololu.Usc;

namespace F4ToPokeys
{
    public class PololuMaestroServo : BindableObject, IDisposable
    {
        #region Construction
        public PololuMaestroServo()
        {
            RemoveServoCommand = new RelayCommand(executeRemoveServo);
            AddAdditionalPointCommand = new RelayCommand(executeAddAdditionalPoint);
            RemoveAdditionalPointCommand = new RelayCommand(executeRemoveAdditionalPoint, canExecuteRemoveAdditionalPoint);

            minPoint = DefaultMinPoint();
            maxPoint = DefaultMaxPoint();
            AdditionalPointList = new ObservableCollection<PololuMaestroPoint>();
        }

        public void Dispose()
        {
            if (falconGauge != null)
                falconGauge.FalconGaugeChanged -= OnFalconGaugeChanged;
        }
        #endregion

        #region ServoIdList
        [XmlIgnore]
        public List<byte> ServoIdList
        {
            get { return servoIdList; }
            private set
            {
                servoIdList = value;
                RaisePropertyChanged("ServoIdList");
            }
        }
        private List<byte> servoIdList;

        private void refreshServoIdList()
        {
            ServoIdList = AvailableServoIdList()
                .Union(ServoIdAsEnumerable())
                .OrderBy(servoId => servoId)
                .ToList();
        }

        private IEnumerable<byte> AvailableServoIdList()
        {
            if (owner == null || owner.Device == null)
                yield break;

            for (byte availableServoId = 0; availableServoId < owner.Device.servoCount; availableServoId++)
                yield return availableServoId;
        }
        #endregion

        #region ServoId
        public byte? ServoId
        {
            get { return servoId; }
            set
            {
                if (servoId == value)
                    return;
                servoId = value;
                RaisePropertyChanged("ServoId");

                updateStatus();
            }
        }
        private byte? servoId;

        private IEnumerable<byte> ServoIdAsEnumerable()
        {
            if (ServoId.HasValue)
                yield return ServoId.Value;
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

        #region RemoveServoCommand
        [XmlIgnore]
        public RelayCommand RemoveServoCommand { get; private set; }

        private void executeRemoveServo(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemoveServoText, ServoId),
                Translations.Main.RemoveServoCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.ServoList.Remove(this);
            Dispose();
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

                OutputTarget = FalconValueToServoValue(falconValue);
            }
        }
        private float? falconValue;
        #endregion

        #region MinPoint
        public PololuMaestroPoint MinPoint
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

        private PololuMaestroPoint minPoint;
        public const float DefaultFalconMinValue = 0.0F;

        private PololuMaestroPoint DefaultMinPoint()
        {
            PololuMaestroPoint point = new PololuMaestroPoint() { FalconValue = DefaultFalconMinValue, ServoValue = 4000 };
            point.PropertyChanged += OnPointChanged;
            return point;
        }
        #endregion

        #region MaxPoint
        public PololuMaestroPoint MaxPoint
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

        private PololuMaestroPoint maxPoint = new PololuMaestroPoint() { FalconValue = DefaultFalconMaxValue, ServoValue = 8000 };
        public const float DefaultFalconMaxValue = 1.0F;

        private PololuMaestroPoint DefaultMaxPoint()
        {
            PololuMaestroPoint point = new PololuMaestroPoint() { FalconValue = DefaultFalconMaxValue, ServoValue = 8000 };
            point.PropertyChanged += OnPointChanged;
            return point;
        }
        #endregion

        #region AdditionalPointList
        public ObservableCollection<PololuMaestroPoint> AdditionalPointList
        {
            get { return additionalPointList; }
            set
            {
                if (additionalPointList != null)
                {
                    additionalPointList.CollectionChanged -= OnAdditionalPointListChanged;
                    foreach (PololuMaestroPoint additionalPoint in additionalPointList)
                        additionalPoint.PropertyChanged -= OnPointChanged;
                }

                additionalPointList = value;

                if (additionalPointList != null)
                {
                    additionalPointList.CollectionChanged += OnAdditionalPointListChanged;
                    foreach (PololuMaestroPoint additionalPoint in additionalPointList)
                        additionalPoint.PropertyChanged += OnPointChanged;
                }

                RaisePropertyChanged("AdditionalPointList");
                RaisePropertyChanged("Points");
                OnPointChanged(this, null);
            }
        }
        private ObservableCollection<PololuMaestroPoint> additionalPointList;

        private void OnAdditionalPointListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (PololuMaestroPoint additionalPoint in e.NewItems.Cast<PololuMaestroPoint>())
                        additionalPoint.PropertyChanged += OnPointChanged;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (PololuMaestroPoint additionalPoint in e.OldItems.Cast<PololuMaestroPoint>())
                        additionalPoint.PropertyChanged -= OnPointChanged;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (PololuMaestroPoint additionalPoint in e.OldItems.Cast<PololuMaestroPoint>())
                        additionalPoint.PropertyChanged -= OnPointChanged;
                    foreach (PololuMaestroPoint additionalPoint in e.NewItems.Cast<PololuMaestroPoint>())
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
            PololuMaestroPoint previousPoint = AdditionalPointList.LastOrDefault() ?? MinPoint;
            PololuMaestroPoint nextPoint = MaxPoint;
            PololuMaestroPoint additionalPoint = new PololuMaestroPoint()
                {
                    FalconValue = (previousPoint.FalconValue + nextPoint.FalconValue) / 2,
                    ServoValue = (ushort)((previousPoint.ServoValue + nextPoint.ServoValue) / 2)
                };
            AdditionalPointList.Add(additionalPoint);
        }
        #endregion

        #region RemoveAdditionalPointCommand
        [XmlIgnore]
        public RelayCommand RemoveAdditionalPointCommand { get; private set; }

        private void executeRemoveAdditionalPoint(object o)
        {
            PololuMaestroPoint additionalPoint = (PololuMaestroPoint)o;
            AdditionalPointList.Remove(additionalPoint);
        }

        private bool canExecuteRemoveAdditionalPoint(object o)
        {
            PololuMaestroPoint additionalPoint = (PololuMaestroPoint)o;
            return AdditionalPointList.Contains(additionalPoint);
        }
        #endregion

        #region OnPointChanged
        private void OnPointChanged(object sender, EventArgs e)
        {
            UpdateOutputTargetRange();
            OutputTarget = FalconValueToServoValue(FalconValue);
        }
        #endregion

        #region Points
        public IEnumerable<PololuMaestroPoint> Points
        {
            get
            {
                yield return MinPoint;

                foreach (PololuMaestroPoint additionalPoint in AdditionalPointList)
                    yield return additionalPoint;

                yield return MaxPoint;
            }
        }
        #endregion

        #region OutputTarget Range
        [XmlIgnore]
        public ushort MinServoValue { get; set; }

        [XmlIgnore]
        public ushort MaxServoValue { get; set; }

        [XmlIgnore]
        public double TickFrequency
        {
            get { return (MaxServoValue - MinServoValue) / 10.0; }
        }

        private void UpdateOutputTargetRange()
        {
            MinServoValue = MinPoint.ServoValue;
            MaxServoValue = MaxPoint.ServoValue;

            foreach (PololuMaestroPoint point in Points)
            {
                if (point.ServoValue < MinServoValue)
                    MinServoValue = point.ServoValue;

                if (point.ServoValue > MaxServoValue)
                    MaxServoValue = point.ServoValue;
            }

            RaisePropertyChanged("MinServoValue");
            RaisePropertyChanged("MaxServoValue");
            RaisePropertyChanged("TickFrequency");
        }
        #endregion

        #region OutputTarget
        [XmlIgnore]
        public ushort OutputTarget
        {
            get { return outputTarget; }
            set
            {
                if (outputTarget == value)
                    return;
                outputTarget = value;
                RaisePropertyChanged("OutputTarget");

                updateOutputTarget();
            }
        }
        private ushort outputTarget;
        #endregion

        #region owner
        public void setOwner(PololuMaestro pololuMaestro)
        {
            owner = pololuMaestro;
            updateStatus();
            OnPointChanged(this, null);
        }

        private PololuMaestro owner;
        #endregion

        #region updateStatus
        public void updateStatus()
        {
            refreshServoIdList();

            if (owner == null)
                return;

            if (!ServoId.HasValue)
            {
                Error = null;
            }
            else if (owner.Device == null)
            {
                Error = null;
            }
            else
            {
                try
                {
                    ServoStatus[] servoStatus;
                    owner.Device.getVariables(out servoStatus);
                    Error = null;
                }
                catch (Exception e)
                {
                    Error = e.Message;
                }
            }

            updateOutputTarget();
        }
        #endregion

        #region updateOutputTarget
        private void updateOutputTarget()
        {
            if (string.IsNullOrEmpty(Error) && owner != null && owner.Device != null && ServoId.HasValue)
            {
                try
                {
                    owner.Device.setTarget(ServoId.Value, OutputTarget);
                }
                catch (Exception e)
                {
                    Error = e.Message;
                }
            }
        }
        #endregion

        #region resetFalconValue
        private void resetFalconValue()
        {
            FalconValue = null;
        }
        #endregion

        #region OnFalconGaugeChanged
        private void OnFalconGaugeChanged(object sender, FalconGaugeChangedEventArgs e)
        {
            FalconValue = e.falconValue;
        }
        #endregion

        #region FalconValueToServoValue
        private ushort FalconValueToServoValue(float? falconValue)
        {
            if (!falconValue.HasValue)
                return 0;

            // Clamp value to [Min, Max]
            if (falconValue.Value <= MinPoint.FalconValue)
                return MinPoint.ServoValue;

            if (falconValue.Value >= MaxPoint.FalconValue)
                return MaxPoint.ServoValue;

            // Find the segment that contains falconValue
            PololuMaestroPoint previousPoint = MinPoint;
            PololuMaestroPoint nextPoint = null;

            foreach (PololuMaestroPoint additionalPoint in AdditionalPointList)
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
            return (ushort)(normalizedValue * (nextPoint.ServoValue - previousPoint.ServoValue) + previousPoint.ServoValue);
        }
        #endregion
    }
}
