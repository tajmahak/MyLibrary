using System.Collections;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public sealed class DBTableCollection : ICollection<DBTable>
    {
        public int Count => list.Count;
        public bool IsReadOnly => false;
        private readonly List<DBTable> list = new List<DBTable>();
        private readonly Dictionary<string, DBTable> dictionary = new Dictionary<string, DBTable>();
        public DBTable this[int index] => list[index];
        public DBTable this[string name]
        {
            get
            {
                if (dictionary.TryGetValue(name, out DBTable table))
                {
                    return table;
                }
                throw DBExceptionFactory.UnknownTableException(name);
            }
        }


        public void Add(DBTable item)
        {
            list.Add(item);
            dictionary.Add(item.Name, item);
        }

        public void Clear()
        {
            list.Clear();
            dictionary.Clear();
        }

        public bool Contains(DBTable item)
        {
            return dictionary.ContainsValue(item);
        }

        public bool Contains(string name)
        {
            return dictionary.ContainsKey(name);
        }

        public void CopyTo(DBTable[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DBTable> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public bool Remove(DBTable item)
        {
            if (dictionary.ContainsKey(item.Name))
            {
                dictionary.Remove(item.Name);
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
