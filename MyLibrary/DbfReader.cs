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
    public class DbfReader : IDisposable, IEnumerable<DbfRow>, IEnumerator<DbfRow>
    {
        public DbfReader(string path, Encoding encoding)
        {
            this.encoding = encoding;
            OpenFile(path);
        }

        public static DataTable ToDataTable(string filePath, Encoding encoding)
        {
            DataTable dataTable = new DataTable();
            using (DbfReader dbf = new DbfReader(filePath, encoding))
            {
                foreach (DbfColumn column in dbf.Columns)
                {
                    dataTable.Columns.Add(column.Name, column.ValueType);
                }
                foreach (DbfRow row in dbf)
                {
                    dataTable.Rows.Add(row.Values);
                }
            }
            return dataTable;
        }

        public DbfColumn[] Columns { get; private set; }
        public DbfRow Current { get; private set; }

        private int recordNumber = 1;
        private readonly Encoding encoding;
        private BinaryReader stream;
        private DbfHeaderDescriptor header;
        private readonly Dictionary<string, int> columnDict = new Dictionary<string, int>();
        private readonly ArrayList fields = new ArrayList();


        public int GetColumnIndex(string columnName)
        {
            return columnDict[columnName];
        }

        public IEnumerator<DbfRow> GetEnumerator()
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

        public void Dispose()
        {
            stream?.BaseStream.Dispose();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        object IEnumerator.Current => Current;

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

        private void OpenFile(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream = new BinaryReader(fileStream);

            byte[] buffer = stream.ReadBytes(Marshal.SizeOf(typeof(DbfHeaderDescriptor)));

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            header = (DbfHeaderDescriptor)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DbfHeaderDescriptor));
            handle.Free();

            while ((13 != stream.PeekChar()))
            {
                buffer = stream.ReadBytes(Marshal.SizeOf(typeof(DbfFieldDescriptor)));
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                fields.Add((DbfFieldDescriptor)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DbfFieldDescriptor)));
                handle.Free();
            }

            //!!_stream.BaseStream.Seek(_header.headerLen + 1, SeekOrigin.Begin); - было так, но при чтении одного из DBF файлов смещение +1 было лишним
            stream.BaseStream.Seek(header.headerLen, SeekOrigin.Begin);

            buffer = stream.ReadBytes(header.recordLen);
            BinaryReader recReader = new BinaryReader(new MemoryStream(buffer));

            List<DbfColumn> colList = new List<DbfColumn>();
            foreach (DbfFieldDescriptor field in fields)
            {
                string number = Encoding.UTF8.GetString(recReader.ReadBytes(field.fieldLen));
                switch (field.fieldType)
                {
                    case 'N':
                        colList.Add(new DbfColumn(field.fieldName, typeof(decimal))); break;
                    case 'C':
                        colList.Add(new DbfColumn(field.fieldName, typeof(string))); break;
                    case 'T':
                        colList.Add(new DbfColumn(field.fieldName, typeof(DateTime))); break;
                    case 'D':
                        colList.Add(new DbfColumn(field.fieldName, typeof(DateTime))); break;
                    case 'L':
                        colList.Add(new DbfColumn(field.fieldName, typeof(bool))); break;
                    case 'F':
                        colList.Add(new DbfColumn(field.fieldName, typeof(decimal))); break;
                    case 'M':
                        colList.Add(new DbfColumn(field.fieldName, typeof(byte[]))); break;
                }
            }
            Columns = colList.ToArray();

            for (int i = 0; i < Columns.Length; i++)
            {
                columnDict.Add(Columns[i].Name, i);
            }
        }

        private bool ReadNextRow()
        {
            while (recordNumber < header.numRecords)
            {
                recordNumber++;
                byte[] buffer = stream.ReadBytes(header.recordLen);
                BinaryReader recReader = new BinaryReader(new MemoryStream(buffer));

                if (recReader.ReadChar() == '*')
                {
                    continue;
                }

                object[] values = new object[fields.Count];
                #region Заполнение строки

                string number;
                for (int i = 0; i < fields.Count; i++)
                {
                    DbfFieldDescriptor field = (DbfFieldDescriptor)fields[i];
                    switch (field.fieldType)
                    {
                        case 'N':  // Number
                            number = encoding.GetString(recReader.ReadBytes(field.fieldLen));

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
                            values[i] = encoding.GetString(recReader.ReadBytes(field.fieldLen));
                            break;

                        case 'D': // Date (YYYYMMDD)
                            string year = encoding.GetString(recReader.ReadBytes(4));
                            string month = encoding.GetString(recReader.ReadBytes(2));
                            string day = encoding.GetString(recReader.ReadBytes(2));
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
                            number = encoding.GetString(recReader.ReadBytes(field.fieldLen));
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

                Current = new DbfRow(this, values);
                return true;
            }
            return false;
        }
    }

    public class DbfColumn
    {
        internal DbfColumn(string name, Type type)
        {
            Name = name;
            ValueType = type;
        }

        public string Name { get; private set; }
        public Type ValueType { get; private set; }

        public override string ToString()
        {
            return $"{Name} [{ValueType.Name}]";
        }
    }

    public class DbfRow
    {
        internal DbfRow(DbfReader reader, object[] values)
        {
            this.reader = reader;
            Values = values;
        }

        public object[] Values { get; private set; }
        public object this[int index] => Values[index];
        public object this[string columnName]
        {
            get
            {
                int columnIndex = reader.GetColumnIndex(columnName);
                return Values[columnIndex];
            }
        }
        private readonly DbfReader reader;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct DbfHeaderDescriptor
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
    internal struct DbfFieldDescriptor
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