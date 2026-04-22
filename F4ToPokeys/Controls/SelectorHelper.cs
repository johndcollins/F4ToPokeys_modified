using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace F4ToPokeys
{
    public static class SelectorHelper
    {
        #region SelectIndexInDesignMode
        public static int? GetSelectIndexInDesignMode(DependencyObject obj)
        {
            return (int?)obj.GetValue(SelectIndexInDesignModeProperty);
        }

        public static void SetSelectIndexInDesignMode(DependencyObject obj, int? value)
        {
            obj.SetValue(SelectIndexInDesignModeProperty, value);
        }

        public static readonly DependencyProperty SelectIndexInDesignModeProperty =
            DependencyProperty.RegisterAttached("SelectIndexInDesignMode", typeof(int?), typeof(SelectorHelper), new UIPropertyMetadata(null, SelectIndexInDesignModePropertyChanged));

        private static void SelectIndexInDesignModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Selector selector = sender as Selector;
            if (selector == null)
                return;

            int? index = (int?)e.NewValue;

            if (index.HasValue)
            {
                bool designMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(selector);
                if (designMode)
                {
                    selector.SelectedIndex = index.Value;
                }
            }
        }
        #endregion
    }
}
