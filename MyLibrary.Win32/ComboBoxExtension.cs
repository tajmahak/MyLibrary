using System;
using System.Windows.Forms;

namespace MyLibrary.Win32
{
    public static class ComboBoxExtension
    {
        public static ValueContainer<T> GetSelectedItem<T>(this ComboBox comboBox)
        {
            object item = comboBox.SelectedItem;
            return GetItem<T>(item);
        }

        public static T GetSelectedValue<T>(this ComboBox comboBox)
        {
            return comboBox.GetSelectedItem<T>().Value;
        }

        public static ValueContainer<T> GetItem<T>(this ComboBox comboBox, int index)
        {
            object item = comboBox.Items[index];
            return GetItem<T>(item);
        }

        public static int SelectValue(this ComboBox comboBox, object value)
        {
            if (comboBox.Items.Count > 0 && !(comboBox.Items[0] is IValueContainer))
            {
                throw new NotSupportedException();
            }

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                IValueContainer item = (IValueContainer)comboBox.Items[i];
                if (Equals(item.GetValue(), value))
                {
                    comboBox.SelectedIndex = i;
                    return i;
                }
            }

            comboBox.SelectedIndex = -1;
            return -1;
        }


        private static ValueContainer<T> GetItem<T>(object item)
        {
            if (item != null)
            {
                return (ValueContainer<T>)item;
            }
            return default;
        }
    }
}
