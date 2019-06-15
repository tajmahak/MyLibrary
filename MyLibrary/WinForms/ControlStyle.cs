using MyLibrary.WinForms.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MyLibrary.WinForms
{
    public class ControlStyle
    {
        public void AddStyleControl(Control control, bool recursive = true)
        {
            var controlType = GetControlType(control);
            _styleControls.Add(controlType, control);

            if (recursive)
            {
                foreach (Control childControl in control.Controls)
                {
                    AddStyleControl(childControl, recursive);
                }
            }

        }
        public void ApplyStyle(Control control, bool recursive = true)
        {
            ControlExtension.SetDoubleBuffer(control, true);

            if (control is Form form)
            {
                var style = GetStyle<Form>();
                if (style != null)
                {
                    form.BackColor = style.BackColor;
                    form.Icon = style.Icon;
                }
            }
            else if (control is MyButton myButton)
            {
                var style = GetStyle<MyButton>();
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
                    myButton.TextAlign = style.TextAlign;
                    myButton.PressedColor = style.PressedColor;
                }
            }

            else if (control is MyDataGridView myDataGridView)
            {
                var style = GetStyle<MyDataGridView>();
                if (style != null)
                {
                    myDataGridView.AlternatingRowsDefaultCellStyle = style.AlternatingRowsDefaultCellStyle.Clone();
                    myDataGridView.AutoSizeColumnsMode = style.AutoSizeColumnsMode;
                    myDataGridView.AutoSizeRowsMode = style.AutoSizeRowsMode;
                    myDataGridView.BackgroundColor = style.BackgroundColor;
                    myDataGridView.BorderStyle = style.BorderStyle;
                    myDataGridView.CellBorderStyle = style.CellBorderStyle;
                    myDataGridView.ColumnHeadersBorderStyle = style.ColumnHeadersBorderStyle;
                    myDataGridView.ColumnHeadersDefaultCellStyle = style.ColumnHeadersDefaultCellStyle.Clone();
                    myDataGridView.ColumnHeadersHeight = style.ColumnHeadersHeight;
                    myDataGridView.ColumnHeadersHeightSizeMode = style.ColumnHeadersHeightSizeMode;
                    myDataGridView.ColumnHeadersVisible = style.ColumnHeadersVisible;
                    myDataGridView.DefaultCellStyle = style.DefaultCellStyle.Clone();
                    myDataGridView.EnableHeadersVisualStyles = style.EnableHeadersVisualStyles;
                    myDataGridView.Font = style.Font;
                    myDataGridView.GridColor = style.GridColor;
                    myDataGridView.MultiSelect = style.MultiSelect;
                    myDataGridView.RowHeadersBorderStyle = style.RowHeadersBorderStyle;
                    myDataGridView.RowHeadersDefaultCellStyle = style.RowHeadersDefaultCellStyle.Clone();
                    myDataGridView.RowHeadersVisible = style.RowHeadersVisible;
                    myDataGridView.RowHeadersWidth = style.RowHeadersWidth;
                    myDataGridView.RowHeadersWidthSizeMode = style.RowHeadersWidthSizeMode;
                    myDataGridView.RowsDefaultCellStyle = style.RowsDefaultCellStyle.Clone();
                    myDataGridView.RowTemplate = (DataGridViewRow)style.RowTemplate.Clone();
                    myDataGridView.ScrollBars = style.ScrollBars;
                    myDataGridView.ShowCellErrors = style.ShowCellErrors;
                    myDataGridView.ShowCellToolTips = style.ShowCellToolTips;
                    myDataGridView.ShowEditingIcon = style.ShowEditingIcon;
                    myDataGridView.ShowRowErrors = style.ShowRowErrors;
                    myDataGridView.StableSort = style.StableSort;
                }
            }

            else if (control is Label label)
            {
                var style = GetStyle<Label>();
                if (style != null)
                {
                    label.AutoEllipsis = style.AutoEllipsis;
                    label.BackColor = style.BackColor;
                    label.BorderStyle = style.BorderStyle;
                    label.Font = new Font(style.Font, label.Font.Style);
                    label.ForeColor = style.ForeColor;
                }
            }
            else if (control is Form || control is UserControl || control is TabPage || control is PrintPreviewControl)
            {
                var style = GetStyle<Form>();
                if (style != null)
                {
                    control.BackColor = style.BackColor;
                }
            }

            if (recursive)
            {
                foreach (Control childControl in control.Controls)
                {
                    ApplyStyle(childControl, recursive);
                }
            }
        }

        private T GetStyle<T>() where T : Control
        {
            var type = typeof(T);
            if (_styleControls.TryGetValue(type, out var styleControl))
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
