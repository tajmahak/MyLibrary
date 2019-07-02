using System.Drawing;
using W = Microsoft.Office.Interop.Word;

namespace MyLibrary.Interop.MSOffice
{
    public sealed class WordRange
    {
        public W.Range Range { get; private set; }

        public WordRange(W.Range wRange)
        {
            Range = wRange;
        }

        public void SetFontColor(Color color)
        {
            Range.Font.Color = GetColor(color);
        }
        public void SetBackgrondColor(Color color)
        {
            Range.Shading.BackgroundPatternColor = GetColor(color);
        }
        public void SetAlignment(HorizontalAlignmentEnum alignment = HorizontalAlignmentEnum.Left)
        {
            switch (alignment)
            {
                case HorizontalAlignmentEnum.Left:
                    Range.ParagraphFormat.Alignment = W.WdParagraphAlignment.wdAlignParagraphLeft; break;
                case HorizontalAlignmentEnum.Center:
                    Range.ParagraphFormat.Alignment = W.WdParagraphAlignment.wdAlignParagraphCenter; break;
                case HorizontalAlignmentEnum.Justify:
                    Range.ParagraphFormat.Alignment = W.WdParagraphAlignment.wdAlignParagraphJustify; break;
                case HorizontalAlignmentEnum.Right:
                    Range.ParagraphFormat.Alignment = W.WdParagraphAlignment.wdAlignParagraphRight; break;
            }
        }
        public void SetFont(string name = null, float? size = null, bool? bold = null, bool? italic = null, bool? underline = null)
        {
            if (name != null)
            {
                Range.Font.Name = name;
            }
            if (size != null)
            {
                Range.Font.Size = size.Value;
            }
            if (bold != null)
            {
                Range.Font.Bold = bold.Value ? 1 : 0;
            }
            if (italic != null)
            {
                Range.Font.Italic = italic.Value ? 1 : 0;
            }
            if (underline != null)
            {
                Range.Font.Underline = underline.Value ? W.WdUnderline.wdUnderlineSingle : W.WdUnderline.wdUnderlineNone;
            }
        }

        private static W.WdColor GetColor(Color color)
        {
            var wColor = (W.WdColor)(color.R + (0x100 * color.G) + (0x10000 * color.B));
            return wColor;
        }
    }
}
