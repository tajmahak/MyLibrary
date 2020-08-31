using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    internal sealed class DBRowCollection : ICollection<DBRow>
    {
        public int Count => list.Count;
        public bool IsReadOnly => false;
        private readonly List<DBRow> list = new List<DBRow>();
        private readonly HashSet<DBRow> hashSet = new HashSet<DBRow>();
        public DBRow this[int index] => list[index];

        public void Add(DBRow item)
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

        public int Clear(Predicate<DBRow> match)
        {
            // вероятно, операция добавления работает быстрее, чем List<>.Remove
            List<DBRow> findList = list.FindAll(x => !match(x));
            if (findList.Count != findList.Count)
            {
                Clear();
                foreach (DBRow item in findList)
                {
                    Add(item);
                }
            }
            return findList.Count - findList.Count;
        }

        public bool Contains(DBRow item)
        {
            return hashSet.Contains(item);
        }

        public void CopyTo(DBRow[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DBRow> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public bool Remove(DBRow item)
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
