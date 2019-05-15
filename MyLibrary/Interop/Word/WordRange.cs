using System.Drawing;
using W = Microsoft.Office.Interop.Word;

namespace MyLibrary.Interop.Word
{
    public class WordRange
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

        private static W.WdColor GetColor(Color color)
        {
            var wColor = (W.WdColor)(color.R + 0x100 * color.G + 0x10000 * color.B);
            return wColor;
        }
    }
}
