using System;
using System.Collections.Generic;

namespace MyLibrary.DataBase
{
    public sealed class DBQueryStructureBlockCollection : List<DBQueryStructureBlock>
    {
        public void Add(DBQueryStructureType type, params object[] args)
        {
            Add(new DBQueryStructureBlock
            {
                Type = type,
                Args = args,
            });
        }

        public List<DBQueryStructureBlock> FindAll(Predicate<DBQueryStructureType> predicate)
        {
            return FindAll(x => predicate(x.Type));
        }

        public List<DBQueryStructureBlock> FindAll(DBQueryStructureType type)
        {
            return FindAll(x => x == type);
        }

        public List<DBQueryStructureBlock> FindAll(Predicate<string> predicate)
        {
            return FindAll(x => predicate(x.Type.ToString()));
        }

        public DBQueryStructureBlock Find(Predicate<DBQueryStructureType> type)
        {
            return Find(x => type(x.Type));
        }

        public DBQueryStructureBlock Find(DBQueryStructureType type)
        {
            return Find(x => x == type);
        }
    }
}
