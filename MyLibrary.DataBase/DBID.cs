using System;

namespace MyLibrary.DataBase
{
    public struct DBID<T> : IEquatable<DBID<T>>, IEquatable<T> where T : struct
    {
        private DBID(T id, Guid tempId, bool hasId)
        {
            this.id = id;
            this.tempId = tempId;
            this.hasId = hasId;
        }

        public static DBID<T> Create(T id)
        {
            return new DBID<T>(id, new Guid(), true);
        }
       
        public static DBID<T> CreateTemp()
        {
            return new DBID<T>(default, Guid.NewGuid(), false);
        }

        private readonly T id;
        private readonly Guid tempId;
        private readonly bool hasId;

        public T Id
        {
            get
            {
                if (hasId)
                {
                    return id;
                }
                throw new NullReferenceException();
            }
        }
        public bool HasId => hasId;


        public bool Equals(DBID<T> other)
        {
            if (HasId && other.HasId)
            {
                return id.Equals(other.id);
            }
            else if (!HasId && !other.HasId)
            {
                return tempId.Equals(other.tempId);
            }
            return false;
        }

        public bool Equals(T other)
        {
            return HasId ? id.Equals(other) : false;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && hasId)
            {
                return id.Equals(obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return hasId ? id.GetHashCode() : tempId.GetHashCode();
        }

        public override string ToString()
        {
            return hasId ? id.ToString() : tempId.ToString();
        }


        public static implicit operator DBID<T>(T value)
        {
            return Create(value);
        }
        public static explicit operator T(DBID<T> value)
        {
            return value.Id;
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
}
