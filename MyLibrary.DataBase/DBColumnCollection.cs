using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public sealed class DBColumnCollection : ICollection<DBColumn>
    {
        public int Count => list.Count;
        public bool IsReadOnly => false;

        private readonly List<DBColumn> list = new List<DBColumn>();
        private readonly Dictionary<string, DBColumn> dictionary = new Dictionary<string, DBColumn>();


        public DBColumn this[int index] => list[index];

        public DBColumn this[string fullName]
        {
            get
            {
                if (dictionary.TryGetValue(fullName, out DBColumn column))
                {
                    return column;
                }
                throw DBExceptionFactory.UnknownColumnException(null, fullName);
            }
        }

        public void Add(DBColumn item)
        {
            if (!dictionary.ContainsKey(item.FullName))
            {
                dictionary.Add(item.FullName, item);
            }
            // колонки могут быть с одинаковыми названиями
            list.Add(item);
        }

        public void Clear()
        {
            list.Clear();
            dictionary.Clear();
        }

        public bool Contains(DBColumn item)
        {
            return dictionary.ContainsValue(item);
        }

        public bool Contains(string fullName)
        {
            return dictionary.ContainsKey(fullName);
        }

        public void CopyTo(DBColumn[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DBColumn> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public DBColumn Find(Predicate<DBColumn> match)
        {
            return list.Find(match);
        }

        public bool Remove(DBColumn item)
        {
            if (dictionary.ContainsKey(item.FullName))
            {
                dictionary.Remove(item.FullName);
            }
            return list.Remove(item);
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
