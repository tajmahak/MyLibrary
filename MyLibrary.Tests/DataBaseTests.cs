using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyLibrary.DataBase;
using MyLibrary.Tests.Properties;
using System;

namespace MyLibrary.Tests
{
    [TestClass]
    public class DataBaseTests
    {
        [TestMethod]
        public void TestSqlWhere()
        {
            var query = CreateQuery();
            query.Where(x => x.Id == 5);
            CheckQuery(query, Resource1.where1);

            query = CreateQuery();
            query.Where(x => x.Id != 5);
            CheckQuery(query, Resource1.where2);
        }


        private FireBirdDBModel CreateModel()
        {
            var model = new FireBirdDBModel();
            model.Initialize(new Type[]
            {
                typeof(TestTable1),
                typeof(TestTable2),
            });
            return model;
        }
        private DBQuery<TestTable1> CreateQuery()
        {
            var query = new DBQuery<TestTable1>(_model.GetTable("TABLE1"));
            return query;
        }
        private DBCompiledQuery CompileQuery(DBQueryBase query)
        {
            return _model.CompileQuery(query);
        }
        private void CheckQuery(DBQueryBase query, string cmd)
        {
            var cQuery = CompileQuery(query);
            Assert.AreEqual(cQuery.CommandText, cmd);
        }
        private DBModelBase _model;
        public DataBaseTests()
        {
            _model = CreateModel();
        }
    }

    [DBOrmTable("TABLE1")]
    internal class TestTable1 : DBOrmTableBase
    {
        [DBOrmColumn("TABLE1.ID", PrimaryKey: true)]
        public long Id { get; set; }
        [DBOrmColumn("TABLE1.TEXT")]
        public string Text { get; set; }

        public TestTable1(DBRow row) : base(row) { }
    }
    [DBOrmTable("TABLE2")]
    internal class TestTable2 : DBOrmTableBase
    {
        [DBOrmColumn("TABLE2.ID", PrimaryKey: true)]
        public long Id { get; set; }
        [DBOrmColumn("TABLE2.TEXT")]
        public string Text { get; set; }
        [DBOrmColumn("TABLE2.TABLE1_ID", ForeignKey: "TABLE1.ID")]
        public long Table1_Id { get; set; }

        public TestTable2(DBRow row) : base(row) { }
    }
}
