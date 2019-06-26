using MyLibrary.Data;
using MyLibrary.Interop;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace MyLibrary.WinForms
{
    public static class ControlExtension
    {
        /// <summary>
        /// Переключение фокуса на окно уже открытого экземпляра программы
        /// </summary>
        /// <returns></returns>
        public static bool FocusProgram()
        {
            var exePath = Assembly.GetEntryAssembly().Location;
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            processes = Array.FindAll(processes, x => x.Id != currentProcess.Id);
            foreach (var process in processes)
            {
                if (string.Equals(exePath, currentProcess.MainModule.FileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Обработка события KeyPress при вводе текста согласно указанному формату вводимых данных
        /// </summary>
        /// <param name="e"></param>
        /// <param name="formatType"></param>
        public static void ProcessKeyPress(KeyPressEventArgs e, Type formatType)
        {
            if (formatType == null)
            {
                return;
            }

            // вещественные типы
            if (formatType == typeof(decimal) || formatType == typeof(double) || formatType == typeof(float))
            {
                if (e.KeyChar == '.')
                {
                    e.KeyChar = ',';
                }

                var c = e.KeyChar;
                e.Handled = !(char.IsControl(c) || char.IsDigit(c) || c == '-' || c == ',');
            }

            // целочисленные типы
            else if (formatType == typeof(long) || formatType == typeof(int) || formatType == typeof(short) || formatType == typeof(byte))
            {
                if (e.KeyChar == '.')
                {
                    e.KeyChar = ',';
                }

                var c = e.KeyChar;
                e.Handled = !(char.IsControl(c) || char.IsDigit(c) || c == '-');
            }
        }
        /// <summary>
        /// Обработка события KeyPress при вводе текста согласно указанному формату вводимых данных в DataGridView
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="e"></param>
        public static void ProcessKeyPress(DataGridView grid, KeyPressEventArgs e)
        {
            var gridCell = grid.GetSelectedCell();
            if (gridCell != null)
            {
                var editingControl = (TextBox)grid.EditingControl;
                if (editingControl != null)
                {
                    ProcessKeyPress(e, gridCell.ValueType);
                }
            }
        }

        /// <summary>
        /// Выполняет указанный делегат в том потоке, которому принадлежит базовый дескриптор
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        public static void InvokeEx(this Control control, MethodInvoker action)
        {
            if (!control.IsDisposed && control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Задает значение, указывающее, должна ли поверхность этого элемента управления перерисовываться с помощью дополнительного буфера, чтобы уменьшить или предотвратить мерцание.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="value"></param>
        public static void SetDoubleBuffer(Control control, bool value)
        {
            Format.SetValue(control, "DoubleBuffered", value);
        }
    }
}
