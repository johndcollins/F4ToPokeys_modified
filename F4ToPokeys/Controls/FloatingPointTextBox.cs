using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace F4ToPokeys
{
    /// <summary>
    /// A control that can be used to edit a floating point value when bound to a <see cref="float"/>
    /// or <see cref="double"/> property and the binding has a <see cref="BindingBase.StringFormat"/> defined.
    /// </summary>
    /// <remarks>
    /// The standard <see cref="TextBox"/> does not operate very well on a binding to a floating point property
    /// that has a <see cref="BindingBase.StringFormat"/> set. For example, if you highlight the existing value
    /// and type a new value over it, when you type the decimal point a second one is added to the text.
    /// This class handles that issue and others to provide a more natural user experience.
    /// <para>
    /// Currently, this class handles the standard format strings <c>F</c> and <c>N</c> with optional format specifier,
    /// and custom format strings comprised of <c>0</c> and/or <c>#</c> placeholders and a decimal point (for example, <c>#00.0#</c>).
    /// Notice these custom format strings must have at least one <c>0</c> after the decimal point.
    /// </para>
    /// </remarks>
    public class FloatingPointTextBox : TextBox
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatingPointTextBox"/> class.
        /// </summary>
        public FloatingPointTextBox()
        {
            Loaded += TextBox_Loaded;
        }
        /// <summary>
        /// Load decimal separator from current UI context.
        /// </summary>
        static FloatingPointTextBox()
        {
            DecimalSeparator = '.';

            string decimalSeparatorString = System.Threading.Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            if (decimalSeparatorString.Length == 1)
                DecimalSeparator = decimalSeparatorString[0];
        }

        protected bool IsManagingInput { get; set; }
        protected bool RequireNegativeHandling { get; set; }
        protected bool NegativeIntegerPending { get; set; }
        protected bool NegativeFractionPending { get; set; }
        protected static char DecimalSeparator { get; set; }

        protected void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // If the StringFormat indicates a floating-point number, we'll perform our special tricks
            IsManagingInput = false;

            var binding = BindingOperations.GetBindingBase(this, TextBox.TextProperty);
            var stringFormat = binding?.StringFormat;

            if (!string.IsNullOrWhiteSpace(stringFormat))
            {
                // If StringFormat is of the form {0:format}, pull out the format stuff
                if (stringFormat[0] == '{')
                {
                    var begin = stringFormat.IndexOf(':');
                    var end = stringFormat.IndexOf('}');
                    if (begin != -1 && end != -1)
                    {
                        stringFormat = stringFormat.Substring(begin + 1, end - begin - 1);
                    }
                }

                switch (stringFormat[0])
                {
                    // Standard format strings: F or N with optional precision specifier
                    case 'f':
                    case 'F':
                    case 'n':
                    case 'N':
                        IsManagingInput = true;
                        break;

                    // Custom format strings: Combinations of 0 and # with a decimal point,
                    // (As long as there is at least one 0 after the decimal point)
                    case '0':
                    case '#':
                        // Validate the full string
                        // (We're not handling exponential notation, percent, or per mille)
                        bool foundDecimalPoint = false;
                        bool foundZeroAfterDecimalPoint = false;

                        for (int i = 1; i < stringFormat.Length; i++)
                        {
                            if (stringFormat[i] == DecimalSeparator)
                            {
                                foundDecimalPoint = true;
                            }
                            else if (stringFormat[i] != '0' && stringFormat[i] != '#')
                            {
                                break;
                            }
                            else if (foundDecimalPoint)
                            {
                                if (stringFormat[i] == '0')
                                {
                                    foundZeroAfterDecimalPoint = true;
                                }
                            }
                        }

                        // Okay, the format string is valid
                        if (foundDecimalPoint && foundZeroAfterDecimalPoint)
                        {
                            IsManagingInput = true;

                            // This type of format string needs extra help with negative numbers
                            if (stringFormat[stringFormat.Length - 1] != DecimalSeparator)
                            {
                                RequireNegativeHandling = true;
                            }
                        }

                        break;
                }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (IsManagingInput)
            {
                NegativeIntegerPending = false;
                NegativeFractionPending = false;

                switch (e.Key)
                {
                    case Key.OemPeriod:
                    case Key.Decimal:
                        // If the user typed a period and we're to the left of the decimal point, skip over the decimal point
                        if (CaretIndex < Text.Length && Text[CaretIndex] == DecimalSeparator)
                        {
                            CaretIndex++;
                            e.Handled = true;
                        }
                        // Or if there is already a decimal point, don't add a second one
                        // (If user is replacing the part that includes the decimal point, accept it)
                        else if (Text.Contains(DecimalSeparator.ToString()) && !SelectedText.Contains(DecimalSeparator.ToString()))
                        {
                            e.Handled = true;
                        }
                        break;

                    case Key.Back:
                        // If the user pressed Backspace and we're to the right of the only decimal point, skip over the decimal point
                        if (CaretIndex > 0 && CaretIndex <= Text.Length && Text[CaretIndex - 1] == DecimalSeparator && Text.HasOnlyOne(DecimalSeparator))
                        {
                            CaretIndex--;
                            e.Handled = true;
                        }
                        break;

                    case Key.Delete:
                        // If the user pressed Delete and we're to the left of the only decimal point, skip over the decimal point
                        if (CaretIndex < Text.Length && Text[CaretIndex] == DecimalSeparator && Text.HasOnlyOne(DecimalSeparator))
                        {
                            CaretIndex++;
                            e.Handled = true;
                        }
                        break;

                    case Key.D0:
                    case Key.NumPad0:
                    case Key.D1:
                    case Key.NumPad1:
                    case Key.D2:
                    case Key.NumPad2:
                    case Key.D3:
                    case Key.NumPad3:
                    case Key.D4:
                    case Key.NumPad4:
                    case Key.D5:
                    case Key.NumPad5:
                    case Key.D6:
                    case Key.NumPad6:
                    case Key.D7:
                    case Key.NumPad7:
                    case Key.D8:
                    case Key.NumPad8:
                    case Key.D9:
                    case Key.NumPad9:
                        if (RequireNegativeHandling)
                        {
                            // If a new negative number is being formed, remember that state so we can correct
                            // the carat position after the text is updated
                            if (Text == "-")
                            {
                                NegativeIntegerPending = true;
                            }
                            else if (Text == "-.")
                            {
                                NegativeFractionPending = true;
                            }
                        }
                        break;

                    case Key.OemMinus:
                    case Key.Subtract:
                        // Only accept a negative sign if:
                        // - We are at the beginning of the number and there is not already a negative sign there, or
                        // - The user has highlighted the entire number, so it is all going to be replaced
                        // NOTE: TextBox does not allow you to enter -0.0, not sure how to override that...
                        if (!((CaretIndex == 0 && Text.IndexOf('-') == -1) ||
                              SelectionLength == Text.Length))
                        {
                            e.Handled = true;
                        }
                        break;
                }
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            // If we've determined this negative number requires caret adjustment, adjust it
            int decimalIndex = Text.IndexOf(DecimalSeparator);
            if (NegativeIntegerPending && decimalIndex > -1)
            {
                CaretIndex = decimalIndex;
            }
            else if (NegativeFractionPending && decimalIndex > -1)
            {
                CaretIndex = decimalIndex + 2;
            }
        }
    }
}
