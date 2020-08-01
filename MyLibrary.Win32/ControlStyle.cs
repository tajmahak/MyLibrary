using MyLibrary.Win32.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MyLibrary.Win32
{
    public class ControlStyle
    {
        public void AddStyleControl(Control control, bool recursive = true)
        {
            Type controlType = GetControlType(control);
            _styleControls.Add(controlType, control);

            if (recursive)
            {
                foreach (Control childControl in control.Controls)
                {
                    AddStyleControl(childControl, recursive);
                }
            }

        }
        public void ApplyStyle(Control control, bool recursive, params Control[] excludeControls)
        {
            ControlExtension.SetDoubleBuffer(control, true);

            if (Array.Exists(excludeControls, x => x == control))
            {
                return;
            }

            if (control is Form form)
            {
                Form style = GetStyle<Form>();
                if (style != null)
                {
                    form.BackColor = style.BackColor;
                    form.Icon = style.Icon;
                }
            }
            else if (control is MyButton myButton)
            {
                MyButton style = GetStyle<MyButton>();
                if (style != null)
                {
                    myButton.AutoEllipsis = style.AutoEllipsis;
                    myButton.BackColor = style.BackColor;
                    myButton.BorderColor = style.BorderColor;
                    myButton.BorderThickness = style.BorderThickness;
                    myButton.DisableBorderColor = style.DisableBorderColor;
                    myButton.DisabledBackgroundColor = style.DisabledBackgroundColor;
                    myButton.DisableForeColor = style.DisableForeColor;
                    myButton.DrawFocusedBorder = style.DrawFocusedBorder;
                    myButton.EnterColor = style.EnterColor;
                    myButton.FixPress = style.FixPress;
                    myButton.Font = style.Font;
                    myButton.ForeColor = style.ForeColor;
                    myButton.OffsetOnClick = style.OffsetOnClick;
                    //myButton.TextAlign = style.TextAlign;
                    myButton.PressedColor = style.PressedColor;
                }
            }

            else if (control is MyDataGridView myDataGridView)
            {
                MyDataGridView style = GetStyle<MyDataGridView>();
                if (style != null)
                {
                    myDataGridView.AlternatingRowsDefaultCellStyle = style.AlternatingRowsDefaultCellStyle.Clone();
                    //myDataGridView.AutoSizeColumnsMode = style.AutoSizeColumnsMode;
                    //myDataGridView.AutoSizeRowsMode = style.AutoSizeRowsMode;
                    myDataGridView.BackgroundColor = style.BackgroundColor;
                    myDataGridView.BorderStyle = style.BorderStyle;
                    myDataGridView.CellBorderStyle = style.CellBorderStyle;
                    myDataGridView.ColumnHeadersBorderStyle = style.ColumnHeadersBorderStyle;
                    myDataGridView.ColumnHeadersDefaultCellStyle = style.ColumnHeadersDefaultCellStyle.Clone();
                    myDataGridView.ColumnHeadersHeight = style.ColumnHeadersHeight;
                    myDataGridView.ColumnHeadersHeightSizeMode = style.ColumnHeadersHeightSizeMode;
                    myDataGridView.DefaultCellStyle = style.DefaultCellStyle.Clone();
                    myDataGridView.EnableHeadersVisualStyles = style.EnableHeadersVisualStyles;
                    myDataGridView.Font = style.Font;
                    myDataGridView.GridColor = style.GridColor;
                    //myDataGridView.MultiSelect = style.MultiSelect;
                    myDataGridView.RowHeadersBorderStyle = style.RowHeadersBorderStyle;
                    myDataGridView.RowHeadersDefaultCellStyle = style.RowHeadersDefaultCellStyle.Clone();
                    //myDataGridView.RowHeadersVisible = style.RowHeadersVisible;
                    myDataGridView.RowHeadersWidth = style.RowHeadersWidth;
                    myDataGridView.RowHeadersWidthSizeMode = style.RowHeadersWidthSizeMode;
                    myDataGridView.RowsDefaultCellStyle = style.RowsDefaultCellStyle.Clone();
                    myDataGridView.RowTemplate = (DataGridViewRow)style.RowTemplate.Clone();
                    myDataGridView.ScrollBars = style.ScrollBars;
                    myDataGridView.ShowCellErrors = style.ShowCellErrors;
                    myDataGridView.ShowCellToolTips = style.ShowCellToolTips;
                    myDataGridView.ShowEditingIcon = style.ShowEditingIcon;
                    myDataGridView.ShowRowErrors = style.ShowRowErrors;
                }
            }

            else if (control is Label label)
            {
                Label style = GetStyle<Label>();
                if (style != null)
                {
                    label.BackColor = style.BackColor;
                    label.BorderStyle = style.BorderStyle;
                    label.ForeColor = style.ForeColor;
                }
            }
            else if (control is Form || control is UserControl || control is TabPage || control is PrintPreviewControl)
            {
                Form style = GetStyle<Form>();
                if (style != null)
                {
                    control.BackColor = style.BackColor;
                }
            }

            if (recursive)
            {
                foreach (Control childControl in control.Controls)
                {
                    ApplyStyle(childControl, recursive, excludeControls);
                }
            }
        }

        private T GetStyle<T>() where T : Control
        {
            Type type = typeof(T);
            if (_styleControls.TryGetValue(type, out Control styleControl))
            {
                return (T)styleControl;
            }
            return default;
        }
        private Type GetControlType(Control control)
        {
            if (control is Form)
            {
                return typeof(Form);
            }
            return control.GetType();
        }

        private readonly Dictionary<Type, Control> _styleControls = new Dictionary<Type, Control>();
    }
}
