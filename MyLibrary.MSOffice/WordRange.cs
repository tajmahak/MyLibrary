using System.Drawing;
using Word = Microsoft.Office.Interop.Word;

namespace MyLibrary.MSOffice
{
    public sealed class WordRange
    {
        public WordRange(Word.Range wRange)
        {
            Range = wRange;
        }

        public Word.Range Range { get; private set; }


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
                    Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft; break;
                case HorizontalAlignmentEnum.Center:
                    Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter; break;
                case HorizontalAlignmentEnum.Justify:
                    Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphJustify; break;
                case HorizontalAlignmentEnum.Right:
                    Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight; break;
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
                Range.Font.Underline = underline.Value ? Word.WdUnderline.wdUnderlineSingle : Word.WdUnderline.wdUnderlineNone;
            }
        }


        private static Word.WdColor GetColor(Color color)
        {
            Word.WdColor wColor = (Word.WdColor)(color.R + (0x100 * color.G) + (0x10000 * color.B));
            return wColor;
        }
    }
}
