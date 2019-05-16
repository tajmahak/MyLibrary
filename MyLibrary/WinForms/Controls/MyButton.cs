using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyLibrary.WinForms.Controls
{
    [System.Diagnostics.DebuggerStepThrough]
    public class MyButton : Button
    {
        #region Свойства

        [DefaultValue(0)]
        public int TextPaddingX
        {
            get { return _textPaddingX; }
            set { _textPaddingX = value; Refresh(); }
        }
        [DefaultValue(-1)]
        public int TextPaddingY
        {
            get { return _textPaddingY; }
            set { _textPaddingY = value; Refresh(); }
        }
        [DefaultValue(0)]
        public int ImagePaddingX
        {
            get { return _imagePaddingX; }
            set { _imagePaddingX = value; Refresh(); }
        }
        [DefaultValue(0)]
        public int ImagePaddingY
        {
            get { return _imagePaddingY; }
            set { _imagePaddingY = value; Refresh(); }
        }

        public Color EnterColor
        {
            get { return _enterColor; }
            set { _enterColor = value; Refresh(); }
        }
        public Color PressedColor
        {
            get { return _pressedColor; }
            set { _pressedColor = value; Refresh(); }
        }
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Refresh(); }
        }
        [DefaultValue(1)]
        public int BorderThickness
        {
            get { return _borderThickness; }
            set { _borderThickness = value; Refresh(); }
        }
        [DefaultValue(true)]
        public bool DrawFocusedBorder
        {
            get { return _useFocusedBorder; }
            set { _useFocusedBorder = value; Refresh(); }
        }

        public Color DisabledBackgroundColor
        {
            get { return _disabledBackgroundColor; }
            set { _disabledBackgroundColor = value; Refresh(); }
        }
        public Color DisableBorderColor
        {
            get { return _disableBorderColor; }
            set { _disableBorderColor = value; Refresh(); }
        }
        public Color DisableForeColor
        {
            get { return _disableForeColor; }
            set { _disableForeColor = value; Refresh(); }
        }

        [DefaultValue(false)]
        public bool FixPress
        {
            get
            {
                return _fixPress;
            }
            set
            {
                _fixPress = value;
                if (_fixPress)
                    Mode = ButtonMode.Press;
                else Mode = ButtonMode.None;
            }
        }
        [DefaultValue(true)]
        public bool OffsetOnClick { get; set; }

        #region private

        private int _textPaddingX;
        private int _textPaddingY;
        private int _imagePaddingX;
        private int _imagePaddingY;

        private Color _enterColor;
        private Color _pressedColor;
        private Color _borderColor;
        private int _borderThickness;
        private bool _useFocusedBorder;
        private bool _fixPress;

        private Color _disabledBackgroundColor;
        private Color _disableBorderColor;
        private Color _disableForeColor;

        #endregion

        #endregion

        public MyButton()
        {
            DoubleBuffered = true;
            TextAlign = ContentAlignment.MiddleCenter;
            ImageAlign = ContentAlignment.MiddleCenter;
            BorderColor = Color.Black;
            BorderThickness = 1;
            TextPaddingY = -1;
            DrawFocusedBorder = true;
            OffsetOnClick = true;
            EnterColor = PressedColor = BackColor;
        }
        protected override void OnPaint(PaintEventArgs pevent)
        {
            int width = Size.Width,
                height = Size.Height;

            var graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.Default;

            #region Отрисовка фона
            {
                Brush brush = null;
                if (Enabled)
                {
                    if (Mode == ButtonMode.Press)
                        brush = new SolidBrush(PressedColor);
                    else if (Mode == ButtonMode.Hot)
                        brush = new SolidBrush(EnterColor);
                    else brush = new SolidBrush(BackColor);
                }
                else
                {
                    brush = new SolidBrush(DisabledBackgroundColor);
                }
                graphics.FillRectangle(brush, 0, 0, width, height);
                brush.Dispose();
            }
            #endregion
            #region Отрисовка изображения
            if (Image != null)
            {
                int x = 0, y = 0;
                #region Выбор расположения

                switch (ImageAlign)
                {
                    case ContentAlignment.TopCenter:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.BottomCenter:
                        x = (width / 2) - (Image.Width / 2); break;

                    case ContentAlignment.TopRight:
                    case ContentAlignment.MiddleRight:
                    case ContentAlignment.BottomRight:
                        x = (width) - (Image.Width); break;
                }
                switch (ImageAlign)
                {
                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.MiddleRight:
                        y = (height / 2) - (Image.Width / 2); break;

                    case ContentAlignment.BottomLeft:
                    case ContentAlignment.BottomCenter:
                    case ContentAlignment.BottomRight:
                        y = (height) - (Image.Height); break;
                }

                #endregion
                x += ImagePaddingX; y += ImagePaddingY;
                if (OffsetOnClick && Mode == ButtonMode.Press)
                { x += 1; y += 1; }

                var image = Image;
                if (!Enabled)
                    image = GetGrayImage(image);
                graphics.DrawImage(image, x, y, image.Width, image.Height);
            }
            #endregion
            #region Отрисовка текста
            {
                TextFormatFlags flags = TextFormatFlags.EndEllipsis;
                #region Выбор расположения
                switch (TextAlign)
                {
                    case ContentAlignment.TopLeft:
                        flags |= TextFormatFlags.Top | TextFormatFlags.Left; break;
                    case ContentAlignment.TopCenter:
                        flags |= TextFormatFlags.Top | TextFormatFlags.HorizontalCenter; break;
                    case ContentAlignment.TopRight:
                        flags |= TextFormatFlags.Top | TextFormatFlags.Right; break;
                    case ContentAlignment.MiddleLeft:
                        flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.Left; break;
                    case ContentAlignment.MiddleCenter:
                        flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter; break;
                    case ContentAlignment.MiddleRight:
                        flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.Right; break;
                    case ContentAlignment.BottomLeft:
                        flags |= TextFormatFlags.Bottom | TextFormatFlags.Left; break;
                    case ContentAlignment.BottomCenter:
                        flags |= TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter; break;
                    case ContentAlignment.BottomRight:
                        flags |= TextFormatFlags.Bottom | TextFormatFlags.Right; break;
                }
                #endregion
                int x = TextPaddingX, y = TextPaddingY;
                if (OffsetOnClick && Mode == ButtonMode.Press)
                { x += 1; y += 1; }

                if (Enabled)
                    TextRenderer.DrawText(graphics, Text, Font, new Rectangle(x, y, Width, Height), ForeColor, flags);
                else TextRenderer.DrawText(graphics, Text, Font, new Rectangle(x, y, Width, Height), DisableForeColor, flags);
            }
            #endregion
            #region Отрисовка границы
            {
                var borderColor = BorderColor;
                var borderWidth = BorderThickness;
                var borderStyle = ButtonBorderStyle.Solid;
                ControlPaint.DrawBorder(graphics, new Rectangle(0, 0, width, height), borderColor, borderWidth, borderStyle, borderColor, borderWidth, borderStyle, borderColor, borderWidth, borderStyle, borderColor, borderWidth, borderStyle);
                if (Focused && DrawFocusedBorder)
                {
                    borderWidth = 1;
                    borderStyle = ButtonBorderStyle.Dashed;
                    ControlPaint.DrawBorder(graphics, new Rectangle(
                        1 + BorderThickness,
                        1 + BorderThickness,
                        width - (2 + (BorderThickness * 2)),
                        height - (2 + (BorderThickness * 2))),
                        borderColor, borderWidth, borderStyle, borderColor, borderWidth, borderStyle, borderColor, borderWidth, borderStyle, borderColor, borderWidth, borderStyle);
                }
            }
            #endregion
        }

        protected override void OnEnter(EventArgs e)
        {
            if (FixPress)
                Mode = ButtonMode.Press;
            base.OnEnter(e);
        }
        protected override void OnLeave(EventArgs e)
        {
            if (FixPress)
                Mode = ButtonMode.Press;
            base.OnLeave(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Mode = ButtonMode.Hot;
            if (FixPress)
                Mode = ButtonMode.Press;

            base.OnMouseEnter(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            lastMouseButton = e.Button;
            if (e.Button == MouseButtons.Right)
                return;
            Mode = ButtonMode.Press;
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                return;
            if (Mode != ButtonMode.None)
                Mode = ButtonMode.Hot;
            if (FixPress)
                Mode = ButtonMode.Press;
            base.OnMouseUp(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            Mode = ButtonMode.None;
            if (FixPress)
                Mode = ButtonMode.Press;
            base.OnMouseLeave(e);
        }

        #region Скрытые сущности

        private enum ButtonMode { None, Hot, Press };
        private ButtonMode _mode;
        private ButtonMode Mode
        {
            get
            { 
                return _mode;
            }
            set
            { 
                _mode = value;
                Refresh(); 
            }
        }

        private MouseButtons lastMouseButton;

        [Browsable(false)]
        new public ImageLayout BackgroundImageLayout { get; set; }

        private Image GetGrayImage(Image image)
        {
            Bitmap bmp = new Bitmap(image);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

            IntPtr ptr = bmpData.Scan0;
            byte[] rgbValues = new byte[bmpData.Stride * bmp.Height];
            int pixelSize = rgbValues.Length / (bmpData.Width * bmpData.Height);

            Marshal.Copy(ptr, rgbValues, 0, rgbValues.Length);
            for (int index = 0; index < rgbValues.Length; index += pixelSize)
            {
                int value = rgbValues[index] + rgbValues[index + 1] + rgbValues[index + 2];
                byte color_value = (byte)(value / 3);

                rgbValues[index] = color_value;
                rgbValues[index + 1] = color_value;
                rgbValues[index + 2] = color_value;
            }
            Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        #endregion
    }
}
