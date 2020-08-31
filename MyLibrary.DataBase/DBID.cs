using System;

namespace MyLibrary.DataBase
{
    public class DBID<T> : IDBID, IEquatable<DBID<T>>, IEquatable<T>
    {
        public DBID(object value)
        {
            if (value is DBTempId tempId)
            {
                TempId = tempId;
            }
            else
            {
                if (!(value is DBNull))
                {
                    Id = (T)value;
                }
            }
        }

        public T Id { get; private set; }
        internal DBTempId TempId { get; private set; }

        public bool Equals(DBID<T> other)
        {
            if (TempId != null && other.TempId != null)
            {
                return Equals(TempId, other.TempId);
            }
            if (TempId == null && other.TempId == null)
            {
                return Equals(Id, other.Id);
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is DBID<T> other)
            {
                return Equals(other);
            }

            if (obj is T value)
            {
                return Equals(value);
            }

            return false;
        }

        public object GetValue()
        {
            if (TempId != null)
            {
                return TempId;
            }
            return Id;
        }

        public bool Equals(T value)
        {
            if (value is DBTempId)
            {
                return TempId != null && Equals(TempId, value);
            }
            return TempId == null && Equals(Id, value);
        }

        public override int GetHashCode()
        {
            if (TempId != null)
            {
                return TempId.GetHashCode();
            }
            if (Id != null)
            {
                return Id.GetHashCode();
            }
            return 0;
        }

        public static implicit operator DBID<T>(T value)
        {
            return new DBID<T>(value);
        }

        public static implicit operator T(DBID<T> value)
        {
            return (T)value.GetValue();
        }

        public static bool operator ==(DBID<T> value1, DBID<T> value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(DBID<T> value1, DBID<T> value2)
        {
            return !value1.Equals(value2);
        }
    }

    internal interface IDBID
    {
        object GetValue();
    }
}
