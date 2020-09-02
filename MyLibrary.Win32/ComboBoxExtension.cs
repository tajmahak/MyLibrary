using System.Windows.Forms;

namespace MyLibrary.Win32
{
    public static class ComboBoxExtension
    {
        public static SelectItem<T> GetSelectedItem<T>(this ComboBox comboBox)
        {
            object item = comboBox.SelectedItem;
            return comboBox.GetItem<T>(item);
        }

        public static SelectItem<T> GetItem<T>(this ComboBox comboBox, int index)
        {
            object item = comboBox.Items[index];
            return comboBox.GetItem<T>(item);
        }

        public static SelectItem<T> GetItem<T>(this ComboBox comboBox, object item)
        {
            if (item != null)
            {
                return (SelectItem<T>)item;
            }
            return default;
        }
    }
}
