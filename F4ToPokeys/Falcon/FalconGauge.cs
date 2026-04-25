using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using F4SharedMem;

namespace F4ToPokeys
{
    #region FalconGauge
    public class FalconGauge
    {
        #region Construction
        public FalconGauge(string label, Func<FlightData, float> getFlightDataProperty, float minValue, float maxValue, int formatTotalSize, int formatIntegralPartMinSize, int formatFractionalPartSize)
        {
            Label = label;
            MinValue = minValue;
            MaxValue = maxValue;
            FormatTotalSize = formatTotalSize;
            FormatIntegralPartMinSize = formatIntegralPartMinSize;
            FormatFractionalPartSize = formatFractionalPartSize;
            this.getFlightDataProperty = getFlightDataProperty;
        }
        #endregion

        #region Label
        public string Label { get; private set; }
        #endregion

        #region MinValue
        public float MinValue { get; private set; }
        #endregion

        #region MaxValue
        public float MaxValue { get; private set; }
        #endregion

        #region FormatTotalSize
        public int FormatTotalSize { get; private set; }
        #endregion

        #region FormatIntegralPartMinSize
        public int FormatIntegralPartMinSize { get; private set; }
        #endregion

        #region FormatFractionalPartSize
        public int FormatFractionalPartSize { get; private set; }
        #endregion

        #region getFlightDataProperty
        private readonly Func<FlightData, float> getFlightDataProperty;
        #endregion

        #region FalconGaugeChanged
        public event EventHandler<FalconGaugeChangedEventArgs> FalconGaugeChanged
        {
            add
            {
                falconGaugeChanged += value;

                ++nbUser;
                if (nbUser == 1)
                    FalconConnector.Singleton.FlightDataChanged += OnFlightDataChanged;
            }

            remove
            {
                falconGaugeChanged -= value;

                --nbUser;
                if (nbUser == 0)
                    FalconConnector.Singleton.FlightDataChanged -= OnFlightDataChanged;
            }
        }

        protected void raiseFalconLightChanged(float? falconValue)
        {
            if (falconGaugeChanged != null)
                falconGaugeChanged(this, new FalconGaugeChangedEventArgs(falconValue));
        }

        private EventHandler<FalconGaugeChangedEventArgs> falconGaugeChanged;
        private int nbUser = 0;
        #endregion

        #region OnFlightDataChanged
        private void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            float? oldValue = getValue(e.oldFlightData);
            float? newValue = getValue(e.newFlightData);

            if (oldValue != newValue)
            {
                raiseFalconLightChanged(newValue);
            }
        }
        #endregion

        #region Value
        private float? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getFlightDataProperty(flightData);
        }
        #endregion
    }
    #endregion

    #region FalconGaugeChangedEventArgs
    public class FalconGaugeChangedEventArgs : EventArgs
    {
        public FalconGaugeChangedEventArgs(float? falconValue)
        {
            this.falconValue = falconValue;
        }

        public readonly float? falconValue;
    }
    #endregion
}
