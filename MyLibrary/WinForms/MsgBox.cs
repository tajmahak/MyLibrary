using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MyLibrary.WinForms
{
    [System.Diagnostics.DebuggerStepThrough]
    public static class MsgBox
    {
        public static DialogResult Show(params object[] items)
        {
            #region Подготовка параметров
            {
                var itemsList = new List<object>();
                for (var i = 0; i < items.Length; i++)
                {
                    if (items[i] is object[])
                    {
                        itemsList.AddRange((object[])items[i]);
                    }
                    else
                    {
                        itemsList.Add(items[i]);
                    }
                }
                items = itemsList.ToArray();
            }
            #endregion

            IWin32Window owner = null;
            string text = "", caption = "";
            var buttons = MessageBoxButtons.OK;
            var icon = MessageBoxIcon.None;
            var defaultButton = MessageBoxDefaultButton.Button1;

            var strings = Array.FindAll(items, x => x is string);
            if (strings.Length > 0)
            {
                text = (string)strings[0];
            }

            if (strings.Length > 1)
            {
                caption = (string)strings[1];
            }

            var enums = Array.Find(items, x => x is IWin32Window);
            if (enums != null)
            {
                owner = (IWin32Window)enums;
            }

            enums = Array.Find(items, x => x is MessageBoxButtons);
            if (enums != null)
            {
                buttons = (MessageBoxButtons)enums;
            }

            enums = Array.Find(items, x => x is MessageBoxIcon);
            if (enums != null)
            {
                icon = (MessageBoxIcon)enums;
            }

            enums = Array.Find(items, x => x is MessageBoxDefaultButton);
            if (enums != null)
            {
                defaultButton = (MessageBoxDefaultButton)enums;
            }

            return MessageBox.Show(owner, text, caption, buttons, icon, defaultButton);
        }
        public static DialogResult ShowError(params object[] items)
        {
            return Show(items, MessageBoxIcon.Error);
        }
        public static DialogResult ShowWarning(params object[] items)
        {
            return Show(items, MessageBoxIcon.Warning);
        }
        public static DialogResult ShowInformation(params object[] items)
        {
            return Show(items, MessageBoxIcon.Information);
        }
        public static DialogResult ShowQuestion(params object[] items)
        {
            return Show(items, MessageBoxIcon.Warning, MessageBoxButtons.YesNo);
        }
        public static DialogResult ShowDeleteRecordQuestion(params object[] items)
        {
            return ShowQuestion(items, "Вы действительно хотите удалить текущую запись?", MessageBoxButtons.YesNo);
        }
    }
}