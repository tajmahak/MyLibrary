using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MyLibrary.Data.Formats
{
    public class DBFReader : IDisposable, IEnumerable<DBFRow>, IEnumerator<DBFRow>
    {
        public static DataTable ToDataTable(string filePath, Encoding encoding)
        {
            var dataTable = new DataTable();
            using (var dbf = new DBFReader(filePath, encoding))
            {
                foreach (var column in dbf.Columns)
                {
                    dataTable.Columns.Add(column.Name, column.ValueType);
                }
                foreach (var row in dbf)
                {
                    dataTable.Rows.Add(row.Values);
                }
            }
            return dataTable;
        }

        public DBFReader(string path, Encoding encoding)
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
            var readed = ReadNextRow();
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
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _stream = new BinaryReader(fileStream);

            var buffer = _stream.ReadBytes(Marshal.SizeOf(typeof(DBFHeader)));

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _header = (DBFHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBFHeader));
            handle.Free();

            while ((13 != _stream.PeekChar()))
            {
                buffer = _stream.ReadBytes(Marshal.SizeOf(typeof(FieldDescriptor)));
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                _fields.Add((FieldDescriptor)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(FieldDescriptor)));
                handle.Free();
            }

            _stream.BaseStream.Seek(_header.headerLen + 1, SeekOrigin.Begin);

            buffer = _stream.ReadBytes(_header.recordLen);
            var recReader = new BinaryReader(new MemoryStream(buffer));

            var colList = new List<DBFColumn>();
            foreach (FieldDescriptor field in _fields)
            {
                var number = Encoding.UTF8.GetString(recReader.ReadBytes(field.fieldLen));
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

            for (var i = 0; i < Columns.Length; i++)
            {
                _columnDict.Add(Columns[i].Name, i);
            }
        }
        private bool ReadNextRow()
        {
            while (_recordNumber < _header.numRecords)
            {
                _recordNumber++;
                var buffer = _stream.ReadBytes(_header.recordLen);
                var recReader = new BinaryReader(new MemoryStream(buffer));

                if (recReader.ReadChar() == '*')
                {
                    continue;
                }

                var values = new object[_fields.Count];
                #region Заполнение строки

                string number;
                for (var i = 0; i < _fields.Count; i++)
                {
                    var field = (FieldDescriptor)_fields[i];
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
                            var year = _encoding.GetString(recReader.ReadBytes(4));
                            var month = _encoding.GetString(recReader.ReadBytes(2));
                            var day = _encoding.GetString(recReader.ReadBytes(2));
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
                            var lDate = recReader.ReadInt32();
                            var lTime = recReader.ReadInt32() * 10000L;
                            values[i] = ToJulianDateTime(lDate).AddTicks(lTime);
                            break;

                        case 'L': // Boolean (Y/N)
                            var boolean = recReader.ReadByte();
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
            var numbers = numberString.ToCharArray();
            var number_count = 0;
            var point_count = 0;
            var space_count = 0;

            foreach (var number in numbers)
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
            var p = Convert.ToDouble(lJDN);
            var s1 = p + 68569;
            var n = Math.Floor(4 * s1 / 146097);
            var s2 = s1 - Math.Floor(((146097 * n) + 3) / 4);
            var i = Math.Floor(4000 * (s2 + 1) / 1461001);
            var s3 = s2 - Math.Floor(1461 * i / 4) + 31;
            var q = Math.Floor(80 * s3 / 2447);
            var d = s3 - Math.Floor(2447 * q / 80);
            var s4 = Math.Floor(q / 11);
            var m = q + 2 - (12 * s4);
            var j = (100 * (n - 49)) + i + s4;
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
                var columnIndex = _reader.GetColumnIndex(columnName);
                return Values[columnIndex];
            }
        }

        internal DBFRow(DBFReader reader, object[] values)
        {
            _reader = reader;
            Values = values;
        }

        private readonly DBFReader _reader;
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