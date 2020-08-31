using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public sealed class DBIndexCollection : ICollection<DBIndex>
    {
        public int Count => list.Count;
        public bool IsReadOnly => false;

        private readonly List<DBIndex> list = new List<DBIndex>();
        private readonly HashSet<DBIndex> hashSet = new HashSet<DBIndex>();

        public DBIndex this[int index] => list[index];

        public void Add(DBIndex item)
        {
            if (!hashSet.Contains(item))
            {
                list.Add(item);
                hashSet.Add(item);
            }
        }

        public void Clear()
        {
            list.Clear();
            hashSet.Clear();
        }

        public bool Contains(DBIndex item)
        {
            return hashSet.Contains(item);
        }

        public void CopyTo(DBIndex[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DBIndex> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public DBIndex Find(Predicate<DBIndex> match)
        {
            return list.Find(match);
        }

        public bool Remove(DBIndex item)
        {
            if (hashSet.Contains(item))
            {
                list.Remove(item);
                hashSet.Remove(item);
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"{nameof(Count)} = {Count}";
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
