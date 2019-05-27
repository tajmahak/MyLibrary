using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace MyLibrary.DataBase
{
    public class FireBirdDBModel : DBModelBase
    {
        public FireBirdDBModel()
        {
            OpenBlock = CloseBlock = '\"';
            ParameterPrefix = '@';
        }

        public override DbCommand CreateCommand(DbConnection connection)
        {
            return ((FbConnection)connection).CreateCommand();
        }
        public override object ExecuteInsertCommand(DbCommand command)
        {
            return command.ExecuteScalar();
        }
        public override void AddCommandParameter(DbCommand command, string name, object value)
        {
            ((FbCommand)command).Parameters.AddWithValue(name, value);
        }
        public override DBCompiledQuery CompileQuery(DBQueryBase query, int nextParameterNumber = 0)
        {
            var cQuery = new DBCompiledQuery()
            {
                NextParameterNumber = nextParameterNumber,
            };

            DBQueryStructureBlock block;
            List<DBQueryStructureBlock> blockList;

            var sql = new StringBuilder();
            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareSelectCommand(sql, query, cQuery);

                block = FindBlock(query, DBQueryStructureTypeEnum.Distinct);
                if (block != null)
                {
                    sql.Insert(6, " DISTINCT");
                }

                block = FindBlock(query, DBQueryStructureTypeEnum.Skip);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" SKIP ", block[0]));
                }

                block = FindBlock(query, DBQueryStructureTypeEnum.First);
                if (block != null)
                {
                    sql.Insert(6, string.Concat(" FIRST ", block[0]));
                }

                PrepareJoinCommand(sql, query);
            }
            else if (query.Type == DBQueryTypeEnum.Insert)
            {
                PrepareInsertCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryTypeEnum.Update)
            {
                PrepareUpdateCommand(sql, query, cQuery);
            }
            else if (query.Type == DBQueryTypeEnum.Delete)
            {
                PrepareDeleteCommand(sql, query);
            }
            else if (query.Type == DBQueryTypeEnum.UpdateOrInsert)
            {
                #region UPDATE OR INSERT

                Add(sql, "UPDATE OR INSERT INTO ", GetName(query.Table.Name));

                blockList = FindBlockList(query, DBQueryStructureTypeEnum.Set);
                if (blockList.Count == 0)
                {
                    throw DBInternal.InadequateUpdateCommandException();
                }

                Add(sql, '(');
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    if (i > 0)
                    {
                        Add(sql, ',');
                    }
                    Add(sql, GetColumnName(block[0]));
                }

                Add(sql, ")VALUES(");
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    if (i > 0)
                    {
                        Add(sql, ',');
                    }
                    Add(sql, AddParameter(block[1], cQuery));
                }

                Add(sql, ')');

                blockList = FindBlockList(query, DBQueryStructureTypeEnum.Matching);
                if (blockList.Count > 0)
                {
                    Add(sql, " MATCHING(");
                    for (int i = 0; i < blockList.Count; i++)
                    {
                        block = blockList[i];
                        for (int j = 0; j < block.Length; j++)
                        {
                            if (j > 0)
                            {
                                Add(sql, ',');
                            }
                            Add(sql, GetColumnName(block[j]));
                        }
                    }
                    Add(sql, ')');
                }

                #endregion
            }

            PrepareWhereCommand(sql, query, cQuery);

            if (query.Type == DBQueryTypeEnum.Select)
            {
                PrepareGroupByCommand(sql, query);
                PrepareOrderByCommand(sql, query);
            }

            #region RETURNING ...

            blockList = FindBlockList(query, DBQueryStructureTypeEnum.Returning);
            if (blockList.Count > 0)
            {
                Add(sql, " RETURNING ");
                for (int i = 0; i < blockList.Count; i++)
                {
                    block = blockList[i];
                    for (int j = 0; j < block.Length; j++)
                    {
                        if (i > 0)
                        {
                            Add(sql, ',');
                        }
                        Add(sql, GetColumnName(block[j]));
                    }
                }
            }

            #endregion

            PrepareUnionCommand(sql, query, cQuery);

            cQuery.CommandText = sql.ToString();
            return cQuery;
        }
        public override Dictionary<string, Type> GetDataTypes()
        {
            var dataTypes = new Dictionary<string, Type>();
            dataTypes.Add("array", typeof(Array));
            dataTypes.Add("bigint", typeof(Int64));
            dataTypes.Add("blob", typeof(Byte[]));
            dataTypes.Add("char", typeof(String));
            dataTypes.Add("date", typeof(DateTime));
            dataTypes.Add("decimal", typeof(Decimal));
            dataTypes.Add("double precision", typeof(Double));
            dataTypes.Add("float", typeof(Single));
            dataTypes.Add("integer", typeof(Int32));
            dataTypes.Add("numeric", typeof(Decimal));
            dataTypes.Add("smallint", typeof(Int16));
            dataTypes.Add("blob sub_type 1", typeof(String));
            dataTypes.Add("time", typeof(TimeSpan));
            dataTypes.Add("timestamp", typeof(DateTime));
            dataTypes.Add("varchar", typeof(String));
            return dataTypes;
        }
        public override string GetDefaultInsertCommand(DBTable table)
        {
          return string.Concat(GetInsertCommand(table), " RETURNING ", GetName(table.Columns[table.PrimaryKeyColumn.Index].Name));
        }
    }
}
