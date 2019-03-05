using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyLibrary.Controls
{
    [System.Diagnostics.DebuggerStepThrough]
    public class MyNumericUpDown : NumericUpDown
    {
        [DefaultValue(false)]
        public bool NextTabOnEnterButton { get; set; }

        [Browsable(false)]
        public decimal RoundValue
        {
            get
            {
                return Math.Round(Value, DecimalPlaces);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            decimal value = Value;
            if (e.Delta > 0)
            {
                if (value > decimal.MaxValue - Increment)
                {
                    if (value != decimal.MaxValue)
                        value = decimal.MaxValue;
                    return;
                }
                value += Increment;
                if (value > Maximum)
                {
                    if (Value != Maximum)
                        Value = Maximum;
                    return;
                }
            }
            else
            {
                if (value < decimal.MinValue + Increment)
                {
                    if (value != decimal.MinValue)
                        value = decimal.MinValue;
                    return;
                }
                value -= Increment;
                if (value < Minimum)
                {
                    if (Value != Minimum)
                        Value = Minimum;
                    return;
                }
            }
            Value = value;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (NextTabOnEnterButton)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = e.SuppressKeyPress = true;
                    SendKeys.Send("{TAB}");
                }
            }
            base.OnKeyDown(e);
        }
    }
}
