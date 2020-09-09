using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace F4ToPokeys
{
    /// <summary>
    /// Logique d'interaction pour SevenSegmentDigitControl.xaml
    /// </summary>
    public partial class SevenSegmentDigitControl : UserControl
    {
        public SevenSegmentDigitControl()
        {
            InitializeComponent();
        }

        private static Brush getDefaultBrush(string name)
        {
            return (Brush)TypeDescriptor.GetConverter(typeof(Brush)).ConvertFromString(name);
        }

        #region Digit
        [Category("Common Properties")]
        public object Digit
        {
            get { return (object)GetValue(DigitProperty); }
            set { SetValue(DigitProperty, value); }
        }

        public static readonly DependencyProperty DigitProperty =
            DependencyProperty.Register("Digit", typeof(object), typeof(SevenSegmentDigitControl), new UIPropertyMetadata(null));
        #endregion

        #region FillOff
        [Category("Common Properties")]
        public Brush FillOff
        {
            get { return (Brush)GetValue(FillOffProperty); }
            set { SetValue(FillOffProperty, value); }
        }

        public static readonly DependencyProperty FillOffProperty =
            DependencyProperty.Register("FillOff", typeof(Brush), typeof(SevenSegmentDigitControl), new UIPropertyMetadata(null));
        #endregion

        #region FillOn
        [Category("Common Properties")]
        public Brush FillOn
        {
            get { return (Brush)GetValue(FillOnProperty); }
            set { SetValue(FillOnProperty, value); }
        }

        public static readonly DependencyProperty FillOnProperty =
            DependencyProperty.Register("FillOn", typeof(Brush), typeof(SevenSegmentDigitControl), new UIPropertyMetadata(getDefaultBrush("LightGreen")));
        #endregion

        #region StrokeThickness
        [Category("Common Properties")]
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(SevenSegmentDigitControl), new UIPropertyMetadata(5.0));
        #endregion

        #region Stroke
        [Category("Common Properties")]
        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(SevenSegmentDigitControl), new UIPropertyMetadata(getDefaultBrush("Black")));
        #endregion

        #region ShowLabel
        [Category("Common Properties")]
        public bool ShowLabel
        {
            get { return (bool)GetValue(ShowLabelProperty); }
            set { SetValue(ShowLabelProperty, value); }
        }

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register("ShowLabel", typeof(bool), typeof(SevenSegmentDigitControl), new UIPropertyMetadata(false));
        #endregion
    }
}
