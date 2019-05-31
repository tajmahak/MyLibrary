using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyLibrary.Data;
using MyLibrary.DataBase;
using System;

namespace MyLibrary.Tests
{
    [TestClass]
    public class DataBaseTests
    {
        private DBModelBase _model;
        public DataBaseTests()
        {
            _model = new FireBirdDBModel();

            Format.SetValue(_model, "OpenBlock", "["); // для удобства тестирования в плане преобразования "" и [] в string
            Format.SetValue(_model, "CloseBlock", "]");

            _model.Initialize(new Type[]
            {
                typeof(OrmTestTable1),
                typeof(OrmTestTable2),
            });
        }
        private DBQuery<OrmTestTable1> CreateQuery()
        {
            var query = new DBQuery<OrmTestTable1>(_model.GetTable("TABLE1"));
            return query;
        }
        private DBCompiledQuery CompileQuery(DBQueryBase query)
        {
            return _model.CompileQuery(query);
        }
        private string GetQuerySql(DBQueryBase query)
        {
            var cQuery = CompileQuery(query);
            return cQuery.CommandText;
        }
        private void CheckWhereQuery(DBQueryBase query, string cmd, params object[] parameters)
        {
            var cQuery = CompileQuery(query);
            if (cQuery.CommandText.StartsWith("SELECT [TABLE1].* FROM [TABLE1] WHERE "))
            {
                cQuery.CommandText = cQuery.CommandText.Remove(0, 38);
            }

            Assert.AreEqual(cQuery.CommandText, cmd);

            if (parameters.Length == 1 && parameters[0] == null)
            {
                // не проверять параметры
                return;
            }

            Assert.AreEqual(cQuery.Parameters.Count, parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                // не обращать внимание на приведение типов
                Assert.AreEqual(cQuery.Parameters[i].Value.ToString(), parameters[i].ToString());
            }
        }


        [TestMethod]
        public void TestSqlWhere()
        {
            var query = CreateQuery();
            query.Where(DbTestTable1.Id, 7);
            var a = GetQuerySql(query);
            CheckWhereQuery(query, "[TABLE1].[ID]=@p0", 7);

            query = CreateQuery();
            query.Where(DbTestTable1.Id, 7, DbTestTable1.Text, "qqq");
            CheckWhereQuery(query, "[TABLE1].[ID]=@p0 AND [TABLE1].[TEXT]=@p1", 7, "qqq");

            query = CreateQuery();
            query.Where(DbTestTable1.Id, 7);
            query.Where(DbTestTable1.Text, "qqq");
            CheckWhereQuery(query, "[TABLE1].[ID]=@p0 AND [TABLE1].[TEXT]=@p1", 7, "qqq");

            query = CreateQuery();
            query.Where(DbTestTable1.Id, "<=", 12);
            CheckWhereQuery(query, "[TABLE1].[ID]<=@p0", 12);

            query = CreateQuery();
            query.Where(x => x.Id == 5);
            CheckWhereQuery(query, "([TABLE1].[ID]=@p0)", 5);

            query = CreateQuery();
            query.Where(x => x.Id != 5);
            CheckWhereQuery(query, "([TABLE1].[ID]<>@p0)", 5);

            query = CreateQuery();
            query.Where(x => x.Id > 5 || x.Id <= 8);
            CheckWhereQuery(query, "(([TABLE1].[ID]>@p0) OR ([TABLE1].[ID]<=@p1))", 5, 8);

            query = CreateQuery();
            query.Where(x => x.Id > 5 | (x.Id != 8 & x.Text == "qqq"));
            CheckWhereQuery(query, "(([TABLE1].[ID]>@p0) OR (([TABLE1].[ID]<>@p1) AND ([TABLE1].[TEXT]=@p2)))", 5, 8, "qqq");
        }




    }

    internal static class DbTestTable1
    {
        public const string _ = "TABLE1";
        public const string Id = "TABLE1.ID";
        public const string Text = "TABLE1.TEXT";
    }
    internal static class DbTestTable2
    {
        public const string _ = "TABLE2";
        public const string Id = "TABLE2.ID";
        public const string Text = "TABLE2.TEXT";
        public const string Table1_Id = "TABLE2.TABLE1_ID";
    }

    [DBOrmTable(DbTestTable1._)]
    internal class OrmTestTable1 : DBOrmTableBase
    {
        [DBOrmColumn(DbTestTable1.Id, PrimaryKey: true)]
        public long Id { get; set; }
        [DBOrmColumn(DbTestTable1.Text)]
        public string Text { get; set; }

        public OrmTestTable1(DBRow row) : base(row) { }
    }
    [DBOrmTable(DbTestTable2._)]
    internal class OrmTestTable2 : DBOrmTableBase
    {
        [DBOrmColumn(DbTestTable2.Id, PrimaryKey: true)]
        public long Id { get; set; }
        [DBOrmColumn(DbTestTable2.Text)]
        public string Text { get; set; }
        [DBOrmColumn(DbTestTable2.Table1_Id, ForeignKey: DbTestTable1.Id)]
        public long Table1_Id { get; set; }

        public OrmTestTable2(DBRow row) : base(row) { }
    }
}
