using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F4ToPokeys
{
    public class SevenSegmentDigitSegment : BindableObject
    {
        #region Value
        private bool myValue;

        public bool Value
        {
            get { return myValue; }
            set
            {
                if (myValue == value)
                    return;
                myValue = value;
                RaisePropertyChanged("Value");

                Dirty = true;
            }
        }
        #endregion

        #region Dirty
        private bool dirty = true;

        public bool Dirty
        {
            get { return dirty; }
            set
            {
                if (dirty == value)
                    return;
                dirty = value;
                RaisePropertyChanged("Dirty");
            }
        }
        #endregion
    }
}
