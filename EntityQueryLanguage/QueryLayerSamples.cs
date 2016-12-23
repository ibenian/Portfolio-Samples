/*

EQL (Entity Query Language) is a fluent API to write SQL queries in a strongly typed fashion
against an object model and execute it against various different databases.  Unlike other ORMs,
EQL supports much of the T-SQL and PL-SQL languages.  Many ORM frameworks aim to completely 
isolate database access by abstracting a common subset of the query languages, while EQL starts with
the database and provides a bridge to OO world with a much richer query language.

*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using NS.Model;
using NS.EQL;   // Do not reference NS.EQL.Model so that EQL namespace is visible when writing tests.  This gives the tester a chance to oversee the namespace that will be used by clients.
using NS.EQL.DataAccess;
using NS.EQL.MSSQL;
using NS.EQL.Oracle;

using PubsObjectModel;
using PubsObjectModel.Meta;
using PubsQueryLayer;

namespace EQLTest.TestClasses
{
    [TestClass]
    public class TestPubsQueryLayer : BaseTestWithEQLDataAccess
    {
        public QueryFactoryClass GetQueryFactory<QueryFactoryClass>() where QueryFactoryClass : EQLQueryFactoryBase
        {
            return this.NewDataAccess.GetQueryFactory<QueryFactoryClass>();
        }

        public QueryFactoryClass GetQueryFactory<QueryFactoryClass>(StatementScope scope) where QueryFactoryClass : EQLQueryFactoryBase
        {
            return this.NewDataAccess.GetQueryFactory<QueryFactoryClass>(scope);
        }

        [TestMethod]
        public void TestSelectByTitleID()
        {
            this.GetQueryFactory<TitleQueryFactory>().GetSelectForTitleLoad("PC8888").DebugDump();
            this.GetQueryFactory<TitleQueryFactory>().GetSelectForTitleLoad("PS2091").DebugDump();
            this.GetQueryFactory<TitleQueryFactory>().GetSelectForTitleLoad("BU1032").DebugDump();

            InsertStatement insert = this.GetQueryFactory<TitleQueryFactory>().GetInsertForTitle();
            insert.DebugDump();
            Title title = new Title("aa", "bb", "cc");
            title.Pubdate = DateTime.Today;
            insert.Scope.RegisterEntitySource("t", title);

            insert.DebugDump();

            List<int> generatedID = new List<int>();
            //insert.Scope.RegisterListResult<int>(0, generatedID);

            insert.DebugDump();

            int id = insert.Execute.InvokeAndReturnGeneratedID<int>();

            Debug.WriteLine(id);
        }

        [TestMethod]
        public void TestSimpleQuery()
        {
            this.GetQueryFactory<TitleQueryFactory>().GetSimpleQuery("PC8888").DebugDump();
            this.GetQueryFactory<TitleQueryFactory>().GetSimpleQuery("PS2091").DebugDump();
            this.GetQueryFactory<TitleQueryFactory>().GetSimpleQuery("BU1032").DebugDump();
        }


        [TestMethod]
        public void TestQueryWithEntitySource()
        {
            Title title1 = new Title("PC8888");
            Title title2 = new Title("PS2091");
            Title title3 = new Title("BU1032");
            this.GetQueryFactory<TitleQueryFactory>().GetQueryWithEntitySource(title1).DebugDump();
            this.GetQueryFactory<TitleQueryFactory>().GetQueryWithEntitySource(title2).DebugDump();
            this.GetQueryFactory<TitleQueryFactory>().GetQueryWithEntitySource(title3).DebugDump();

        }

        [TestMethod]
        public void TestInsertTitle()
        {
            int recordCount = 10;
            try
            {
                for (int i = 0; i < recordCount; i++)
                {
                    Title title = new Title("tit" + i.ToString(), "Title " + i.ToString(), "technology");
                    title.Pubdate = DateTime.Today;
                    InsertStatement insert = this.GetQueryFactory<TitleQueryFactory>().GetInsertTitle(title);
                    insert.DebugDump();
                    int count = insert.Execute.Invoke();
                    Debug.WriteLine(count);
                }
            }
            finally
            {
                TitleMeta TITLE = new TitleMeta();
                int deletedCount = this.NewStatement.Delete(TITLE).Where(TITLE.titleId.StartsWith("tit")).DebugDump(null).Invoke();
                Debug.WriteLine(deletedCount);
                Assert.AreEqual(recordCount, deletedCount, "Deleted rows are not equal to inserted rows!");
            }

            CachedQuery.DumpCachedQueries(false);

        }

    }
}
