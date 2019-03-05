using System.ComponentModel;
using System.Windows.Forms;

namespace MyLibrary.Controls
{
    [System.Diagnostics.DebuggerStepThrough]
    public class MyDataGridView : DataGridView
    {
        public MyDataGridView()
        {
            base.DoubleBuffered = true;
        }

        [DefaultValue(false)]
        public bool NextTabOnEnterButton { get; set; }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (NextTabOnEnterButton && keyData == Keys.Enter)
            {
                base.ProcessTabKey(Keys.Tab);
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
                return false;

            if (e.KeyCode == Keys.Escape)
                return false;
            if (e.Shift && e.KeyCode == Keys.Space)
                return false;
            if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
                return false;
            if (NextTabOnEnterButton && e.KeyCode == Keys.Enter)
            {
                base.ProcessTabKey(Keys.Tab);
                return true;
            }

            return base.ProcessDataGridViewKey(e);
        }
    }
}
