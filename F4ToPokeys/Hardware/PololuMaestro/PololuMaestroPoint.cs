using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F4ToPokeys
{
    public class PololuMaestroPoint : BindableObject
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

        #region ServoValue
        public ushort ServoValue
        {
            get { return servoValue; }
            set
            {
                if (servoValue == value)
                    return;
                servoValue = value;
                RaisePropertyChanged("ServoValue");
            }
        }
        private ushort servoValue;
        #endregion
    }
}
