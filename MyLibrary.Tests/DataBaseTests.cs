using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyLibrary.DataBase.Orm;
using MyLibrary.DataBase;

namespace MyLibrary.Tests
{
    [TestClass]
    public class DataBaseTests
    {
        [TestMethod]
        public void TestSqlWhere()
        {
            var model = CreateModel();

        }

        private FireBirdDBModel CreateModel()
        {
            var model = new FireBirdDBModel();
            model.InitializeFromOrmModel(new Type[]
            {
                typeof(TestTable1),
                typeof(TestTable2),
            });
            return model;
        }
    }





    [DBOrmTable("TABLE1")]
    class TestTable1
    {
        [DBOrmColumn("TABLE1.ID")]
        public long Id { get; set; }
        [DBOrmColumn("TABLE1.TEXT")]
        public string Text { get; set; }
    }
    [DBOrmTable("TABLE2")]
    class TestTable2
    {
        [DBOrmColumn("TABLE2.ID")]
        public long Id { get; set; }
        [DBOrmColumn("TABLE2.TEXT")]
        public string Text { get; set; }
        [DBOrmColumn("TABLE2.TABLE1_ID", "TABLE1.ID")]
        public long Table1_Id { get; set; }
    }



}
