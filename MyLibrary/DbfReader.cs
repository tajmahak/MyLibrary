using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MyLibrary
{
    public class DbfReader : IDisposable, IEnumerable<DBFRow>, IEnumerator<DBFRow>
    {
        public static DataTable ToDataTable(string filePath, Encoding encoding)
        {
            DataTable dataTable = new DataTable();
            using (DbfReader dbf = new DbfReader(filePath, encoding))
            {
                foreach (DBFColumn column in dbf.Columns)
                {
                    dataTable.Columns.Add(column.Name, column.ValueType);
                }
                foreach (DBFRow row in dbf)
                {
                    dataTable.Rows.Add(row.Values);
                }
            }
            return dataTable;
        }

        public DbfReader(string path, Encoding encoding)
        {
            _encoding = encoding;
            OpenFile(path);
        }
        public void Dispose()
        {
            _stream?.BaseStream.Dispose();
        }

        public DBFColumn[] Columns { get; private set; }
        public DBFRow Current { get; private set; }
        public int GetColumnIndex(string columnName)
        {
            return _columnDict[columnName];
        }

        // IEnumerable, IEnumerator
        public IEnumerator<DBFRow> GetEnumerator()
        {
            return this;
        }
        public bool MoveNext()
        {
            bool readed = ReadNextRow();
            return readed;
        }
        public void Reset()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
        object IEnumerator.Current => Current;

        private void OpenFile(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _stream = new BinaryReader(fileStream);

            byte[] buffer = _stream.ReadBytes(Marshal.SizeOf(typeof(DBFHeader)));

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _header = (DBFHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBFHeader));
            handle.Free();

            while ((13 != _stream.PeekChar()))
            {
                buffer = _stream.ReadBytes(Marshal.SizeOf(typeof(FieldDescriptor)));
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                _fields.Add((FieldDescriptor)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(FieldDescriptor)));
                handle.Free();
            }

            //!!_stream.BaseStream.Seek(_header.headerLen + 1, SeekOrigin.Begin); - было так, но при чтении одного из DBF файлов смещение +1 было лишним
            _stream.BaseStream.Seek(_header.headerLen, SeekOrigin.Begin);

            buffer = _stream.ReadBytes(_header.recordLen);
            BinaryReader recReader = new BinaryReader(new MemoryStream(buffer));

            List<DBFColumn> colList = new List<DBFColumn>();
            foreach (FieldDescriptor field in _fields)
            {
                string number = Encoding.UTF8.GetString(recReader.ReadBytes(field.fieldLen));
                switch (field.fieldType)
                {
                    case 'N':
                        colList.Add(new DBFColumn(field.fieldName, typeof(decimal))); break;
                    case 'C':
                        colList.Add(new DBFColumn(field.fieldName, typeof(string))); break;
                    case 'T':
                        colList.Add(new DBFColumn(field.fieldName, typeof(DateTime))); break;
                    case 'D':
                        colList.Add(new DBFColumn(field.fieldName, typeof(DateTime))); break;
                    case 'L':
                        colList.Add(new DBFColumn(field.fieldName, typeof(bool))); break;
                    case 'F':
                        colList.Add(new DBFColumn(field.fieldName, typeof(decimal))); break;
                    case 'M':
                        colList.Add(new DBFColumn(field.fieldName, typeof(byte[]))); break;
                }
            }
            Columns = colList.ToArray();

            for (int i = 0; i < Columns.Length; i++)
            {
                _columnDict.Add(Columns[i].Name, i);
            }
        }
        private bool ReadNextRow()
        {
            while (_recordNumber < _header.numRecords)
            {
                _recordNumber++;
                byte[] buffer = _stream.ReadBytes(_header.recordLen);
                BinaryReader recReader = new BinaryReader(new MemoryStream(buffer));

                if (recReader.ReadChar() == '*')
                {
                    continue;
                }

                object[] values = new object[_fields.Count];
                #region Заполнение строки

                string number;
                for (int i = 0; i < _fields.Count; i++)
                {
                    FieldDescriptor field = (FieldDescriptor)_fields[i];
                    switch (field.fieldType)
                    {
                        case 'N':  // Number
                            number = _encoding.GetString(recReader.ReadBytes(field.fieldLen));

                            if (IsNumber(number))
                            {
                                if (number.IndexOf(".") != -1)
                                {
                                    number = number.Replace(".",
                                        Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                                }
                                values[i] = decimal.Parse(number);
                            }
                            break;

                        case 'C': // String
                            values[i] = _encoding.GetString(recReader.ReadBytes(field.fieldLen));
                            break;

                        case 'D': // Date (YYYYMMDD)
                            string year = _encoding.GetString(recReader.ReadBytes(4));
                            string month = _encoding.GetString(recReader.ReadBytes(2));
                            string day = _encoding.GetString(recReader.ReadBytes(2));
                            values[i] = DBNull.Value;
                            try
                            {
                                if (IsNumber(year) && IsNumber(month) && IsNumber(day))
                                {
                                    if ((int.Parse(year) > 1900))
                                    {
                                        values[i] = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                                    }
                                }
                            }
                            catch { }
                            break;

                        case 'T': // Timestamp, 8 bytes - two integers, first for date, second for time
                            int lDate = recReader.ReadInt32();
                            long lTime = recReader.ReadInt32() * 10000L;
                            values[i] = ToJulianDateTime(lDate).AddTicks(lTime);
                            break;

                        case 'L': // Boolean (Y/N)
                            byte boolean = recReader.ReadByte();
                            switch ((char)boolean)
                            {
                                case 'T':
                                case 'Y':
                                    values[i] = true; break;
                                case 'F':
                                case 'N':
                                    values[i] = false; break;
                            }
                            break;

                        case 'F':
                            number = _encoding.GetString(recReader.ReadBytes(field.fieldLen));
                            if (IsNumber(number))
                            {
                                values[i] = decimal.Parse(number);
                            }
                            break;

                        case 'M':
                            values[i] = recReader.ReadBytes(field.fieldLen);
                            break;
                    }
                }

                #endregion

                Current = new DBFRow(this, values);
                return true;
            }
            return false;
        }
        private static bool IsNumber(string numberString)
        {
            char[] numbers = numberString.ToCharArray();
            int number_count = 0;
            int point_count = 0;
            int space_count = 0;

            foreach (char number in numbers)
            {
                if ((number >= 48 && number <= 57))
                {
                    number_count += 1;
                }
                else if (number == 46)
                {
                    point_count += 1;
                }
                else if (number == 32)
                {
                    space_count += 1;
                }
                else
                {
                    return false;
                }
            }

            return (number_count > 0 && point_count < 2);
        }
        private static DateTime ToJulianDateTime(long lJDN)
        {
            double p = Convert.ToDouble(lJDN);
            double s1 = p + 68569;
            double n = Math.Floor(4 * s1 / 146097);
            double s2 = s1 - Math.Floor(((146097 * n) + 3) / 4);
            double i = Math.Floor(4000 * (s2 + 1) / 1461001);
            double s3 = s2 - Math.Floor(1461 * i / 4) + 31;
            double q = Math.Floor(80 * s3 / 2447);
            double d = s3 - Math.Floor(2447 * q / 80);
            double s4 = Math.Floor(q / 11);
            double m = q + 2 - (12 * s4);
            double j = (100 * (n - 49)) + i + s4;
            return new DateTime(Convert.ToInt32(j), Convert.ToInt32(m), Convert.ToInt32(d));
        }
        private int _recordNumber = 1;
        private readonly Encoding _encoding;
        private BinaryReader _stream;
        private DBFHeader _header;
        private readonly Dictionary<string, int> _columnDict = new Dictionary<string, int>();
        private readonly ArrayList _fields = new ArrayList();
    }

    public class DBFColumn
    {
        public string Name { get; private set; }
        public Type ValueType { get; private set; }

        internal DBFColumn(string name, Type type)
        {
            Name = name;
            ValueType = type;
        }
        public override string ToString()
        {
            return $"{Name} [{ValueType.Name}]";
        }
    }

    public class DBFRow
    {
        public object[] Values { get; private set; }
        public object this[int index] => Values[index];
        public object this[string columnName]
        {
            get
            {
                int columnIndex = _reader.GetColumnIndex(columnName);
                return Values[columnIndex];
            }
        }

        internal DBFRow(DbfReader reader, object[] values)
        {
            _reader = reader;
            Values = values;
        }

        private readonly DbfReader _reader;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct DBFHeader
    {
        public byte version;
        public byte updateYear;
        public byte updateMonth;
        public byte updateDay;
        public int numRecords;
        public short headerLen;
        public short recordLen;
        public short reserved1;
        public byte incompleteTrans;
        public byte encryptionFlag;
        public int reserved2;
        public long reserved3;
        public byte MDX;
        public byte language;
        public short reserved4;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct FieldDescriptor
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
        public string fieldName;
        public char fieldType;
        public int address;
        public byte fieldLen;
        public byte count;
        public short reserved1;
        public byte workArea;
        public short reserved2;
        public byte flag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] reserved3;
        public byte indexFlag;
    }
}