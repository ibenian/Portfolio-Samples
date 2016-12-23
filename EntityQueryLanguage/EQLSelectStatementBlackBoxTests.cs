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
using System.Data;
using NS.Model;
using NS.DataLayer;
using NS.EQL;   // Do not reference NS.EQL.Model so that EQL namespace is visible when writing tests.  This gives the tester a chance to oversee the namespace that will be used by clients.
using NS.EQL.DataAccess;

using PubsObjectModel;
using PubsObjectModel.Meta;


namespace EQLTest.TestClasses
{

    /// <summary>
    /// These tests 
    ///     1. build an eql query.
    ///     2. execute the query.
    ///     3. check the result to see if it contains the expected 'key' data proves the 'tested' feature works correctly.
    /// 
    /// These tests do not require internal knowledge of the EQL model.  We just look into the output to see if it is what we expect to see.
    /// </summary>
    [TestClass]
    public class EQLSelectStatementBlackBoxTests : BaseTestWithEQLDataAccess
    {
        private const string mainTestTitleId = "PS2091";    // titleId to be used in tests


        /// <summary>
        /// The Test:
        ///     Build a select statement that loads a single title by its titleId and fill the result into an object.
        /// Expectations:
        ///     The query must return a resultset.
        ///     The returned result must contain the titleId that we requested. 
        /// Strategy:
        ///     All we want is to determine whether the select statement will return the resultset that we request or not.
        ///     We do not intend to test whether each column will be filled into the object properly or not, because we already know this is tested by another unit test.
        ///     Assume that this test succeeds, but the other fields are not populated correctly. This test will not be able to detect it, but it will verify the fact that
        ///     the query has been built and run correctly.  In other words, if this test passes, it means the query generation and execution is good and it will perform
        ///     good if the rest of the system is also good.
        /// </summary>
        [TestMethod]
        public void TestSelectATitle()
        {
            TitleMeta title = new TitleMeta("t");           // Create meta-info objects that will be used to refer to tables through entities.  Also specify an entity alias to be used in the context of this query.

            EQLDataAccess dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
            StatementScope scope = dataAccess.NewStatement; // Acquire a new StatementScope.

            Title titleObject = new Title();                // Create a new object to fill into.

            // Build the statement

            bool result = scope.From(title)
                .Select(title.MappedColumns)
                .Where(title.titleId == mainTestTitleId)
                .DebugDump("Load a title.")
                .Into(titleObject);

            // Dump the result
            Debug.WriteLine(titleObject);

            // Decide pass or fail based on just the results returned.  Try not to use any temporarily valid assertions here.

            // A result must be available
            Assert.IsTrue(result, "Query returned empty resultset.");

            // Is the object loaded the one we requested?
            Assert.AreEqual(titleObject.TitleId, mainTestTitleId, "Query returned a different record.");

            // We do not check other fields, because we do not have any expectations on the values of other fields.
            // The assumptions are:
            //      if the title.titleId is good, the other fields should be good too,
            //      loading an object from reader has already been tested by another unit test.
        }

        /// <summary>
        /// The Test:
        ///     Build a select statement that loads all the titles into a collection.
        /// Expectations:
        ///     The query must return a resultset.
        ///     The returned resultset must contain exactly the number of records that are in the database.
        /// Strategy:
        ///     All we want is to determine whether the select statement will return the resultset that we request or not.
        ///     How do we know if the returned resultset is good or bad?  One way is to execute a direct SQL statement that counts the rows in the table.
        ///     We don't care if each and every single column is good or bad.
        ///     All we want is that the returned query should contain the number of records that we expect.
        ///     In addition, we could also check if the IDs in the returned resultset are the IDs that are actually in the DB.
        /// </summary>
        [TestMethod]
        public void TestSelectAllTitles()
        {
            TitleMeta title = new TitleMeta("t");           // Create meta-info objects that will be used to refer to tables through entities.  Also specify an entity alias to be used in the context of this query.

            EQLDataAccess dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
            StatementScope scope = dataAccess.NewStatement; // Acquire a new StatementScope.

            TitleCollection titleCol = new TitleCollection();   // Create a new collection to fill into.

            /*int count = scope.From(title)
                .Select(title.MappedColumns)
                .DebugDump("Load all titles.")
                .Into(titleCol);*/

            NS.EQL.Syntax.ISelectClause sel = scope.From(title)
                .Select(title.MappedColumns)
                .DebugDump("Load all titles.");

            int count = sel.Into(titleCol);

            Assert.AreNotEqual(count, 0, "There must be at least 1 record for the unit test to run");
            // Dump the result
            Debug.WriteLine(titleCol);

            // Decide pass or fail based on just the results returned.  Try not to use any temporarily valid assertions here.

            // Find out the record count by executing a manually written SQL query.
            int actualRowCount = Convert.ToInt32(this.NewDataAccess.TextExecScalar(0, "SELECT COUNT(*) FROM TITLES", new IDataParameter[] { }));

            Assert.AreEqual(count, actualRowCount, "The number of records returned from query doesn't match the number of records in table.");

            // May also check whether the loaded IDs are good or not.
        }

        /// <summary>
        /// The Test:
        ///     Build a select statement that loads all the titlesAuthors of a specific title into a collection.
        /// Expectations:
        ///     The query must return a resultset.
        ///     The returned resultset must contain exactly the number of records in the DB.
        ///     The returned resultset must all have the titleAuthor records for the given title but nothing else.
        /// Strategy:
        ///     All we want is to determine whether the select statement will return the resultset that we request or not.
        ///     We can first make sure the record count is the record count that we determine by a second manually written query.
        ///     One way to validate the recrords in the resultset is to check whether each FK (titleId) in the resultset is the one requested.
        /// </summary>
        [TestMethod]
        public void TestSelectAllTitleAuthorsDirectly()
        {
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("tau");       // Create meta-info objects that will be used to refer to tables through entities.  Also specify an entity alias to be used in the context of this query.

            EQLDataAccess dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
            StatementScope scope = dataAccess.NewStatement; // Acquire a new StatementScope.

            TitleAuthorCollection titleAuthorCol = new TitleAuthorCollection();   // Create a new collection to fill into.

            int count = scope.From(titleAuthor)
                .Select(titleAuthor.MappedColumns)
                .Where(titleAuthor.titleId == mainTestTitleId)
                .DebugDump("Load all title authors of a title.")
                .Into(titleAuthorCol);

            Assert.AreNotEqual(count, 0, "There must be at least 1 record for the unit test to run");
            // Dump the result
            Debug.WriteLine(titleAuthorCol);

            // Decide pass or fail based on just the results returned.  Try not to use any temporarily valid assertions here.

            // Find out the record count by executing a manually written SQL query.
            int actualRowCount = Convert.ToInt32(this.NewDataAccess.TextExecScalar(0, "SELECT COUNT(*) FROM TITLEAUTHOR WHERE title_id=@titleID", new IDataParameter[] { CommonDB.Instance.NewParameter("titleId", mainTestTitleId) }));
            Assert.AreEqual(count, actualRowCount, "The number of records returned from query doesn't match the number of records in table.");

            // Now find out whether each titleAuthor in the collection actually belongs to the specified titleId.

            foreach (TitleAuthor titleAuthorElem in titleAuthorCol)
                Assert.AreEqual(titleAuthorElem.TitleId, mainTestTitleId, "Element in the resultset does not belong to the the specified tileId. " + titleAuthorElem.ToString());
        }

        /// <summary>
        /// The Test:
        ///     Build a select statement that loads all the titlesAuthors of a specific title into a collection using title.TitleAuthors relationship.
        ///     Use a custom condition to tie the titleAuthor to the given mainTestTitleId rather than the parent title.titleId.
        /// Expectations:
        ///     The query must return a resultset.
        ///     The returned resultset must contain exactly the number of records in the DB.
        ///     The returned resultset must all have the titleAuthor records for the given title but nothing else.
        /// Strategy:
        ///     All we want is to determine whether the select statement will return the resultset that we request or not.
        ///     We can first make sure the record count is the record count that we determine by a second manually written query.
        ///     One way to validate the recrords in the resultset is to check whether each FK (titleId) in the resultset is the one requested.
        /// </summary>
        [TestMethod]
        public void TestSelectAllTitleAuthorsUsingRelationshipCondition()
        {
            TitleMeta title = new TitleMeta("t");           // Create meta-info objects that will be used to refer to tables through entities.  Also specify an entity alias to be used in the context of this query.

            EQLDataAccess dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
            StatementScope scope = dataAccess.NewStatement; // Acquire a new StatementScope.

            TitleAuthorCollection titleAuthorCol = new TitleAuthorCollection();   // Create a new collection to fill into.

            int count = scope.From(title.TitleAuthors.On(title.TitleAuthors.Right.titleId == mainTestTitleId))      // use given custom criteria instead of tieing the titleAuthor to the parent title.
                .DebugDump("Load all title authors of a title using relationship condition.")
                .Into(titleAuthorCol);

            Assert.AreNotEqual(count, 0, "There must be at least 1 record for the unit test to run");
            // Dump the result
            Debug.WriteLine(titleAuthorCol);

            // Decide pass or fail based on just the results returned.  Try not to use any temporarily valid assertions here.

            // Find out the record count by executing a manually written SQL query.
            int actualRowCount = Convert.ToInt32(this.NewDataAccess.TextExecScalar(0, "SELECT COUNT(*) FROM TITLEAUTHOR WHERE title_id=@titleID", new IDataParameter[] { CommonDB.Instance.NewParameter("titleId", mainTestTitleId) }));
            Assert.AreEqual(count, actualRowCount, "The number of records returned from query doesn't match the number of records in table.");

            // Now find out whether each titleAuthor in the collection actually belongs to the specified titleId.

            foreach (TitleAuthor titleAuthorElem in titleAuthorCol)
                Assert.AreEqual(titleAuthorElem.TitleId, mainTestTitleId, "Element in the resultset does not belong to the the specified tileId. " + titleAuthorElem.ToString());
        }

        /// <summary>
        /// The Test:
        ///     Build a select statement that loads all the titlesAuthors of a specific title into a collection using title.TitleAuthors relationship.
        /// Expectations:
        ///     The query must return a resultset.
        ///     The returned resultset must contain exactly the number of records in the DB.
        ///     The returned resultset must all have the titleAuthor records for the given title but nothing else.
        /// Strategy:
        ///     All we want is to determine whether the select statement will return the resultset that we request or not.
        ///     We can first make sure the record count is the record count that we determine by a second manually written query.
        ///     One way to validate the recrords in the resultset is to check whether each FK (titleId) in the resultset is the one requested.
        /// </summary>
        [TestMethod]
        public void TestSelectAllTitleAuthorsUsingJoinedRelationship()
        {
            TitleMeta title = new TitleMeta("t");           // Create meta-info objects that will be used to refer to tables through entities.  Also specify an entity alias to be used in the context of this query.

            EQLDataAccess dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
            StatementScope scope = dataAccess.NewStatement; // Acquire a new StatementScope.

            TitleAuthorCollection titleAuthorCol = new TitleAuthorCollection();   // Create a new collection to fill into.

            int count = scope.From(title)           // select from parent table first, and then join to the child table
                .SelectJoined(title.TitleAuthors.Right.MappedColumns)       // will be joined and selected
                .Where(title.titleId == mainTestTitleId)
                .DebugDump("Load all title authors of a title using relationship condition.")
                .Into(titleAuthorCol);

            Assert.AreNotEqual(count, 0, "There must be at least 1 record for the unit test to run");
            // Dump the result
            Debug.WriteLine(titleAuthorCol);

            // Decide pass or fail based on just the results returned.  Try not to use any temporarily valid assertions here.

            // Find out the record count by executing a manually written SQL query.
            int actualRowCount = Convert.ToInt32(this.NewDataAccess.TextExecScalar(0, "SELECT COUNT(*) FROM TITLEAUTHOR WHERE title_id=@titleID", new IDataParameter[] { CommonDB.Instance.NewParameter("titleId", mainTestTitleId) }));
            Assert.AreEqual(count, actualRowCount, "The number of records returned from query doesn't match the number of records in table.");

            // Now find out whether each titleAuthor in the collection actually belongs to the specified titleId.

            foreach (TitleAuthor titleAuthorElem in titleAuthorCol)
                Assert.AreEqual(titleAuthorElem.TitleId, mainTestTitleId, "Element in the resultset does not belong to the the specified tileId. " + titleAuthorElem.ToString());
        }

        /// <summary>
        /// The Test:
        ///     Build a select statement that loads all the authors who has at least 1 title in business.
        /// Expectations:
        ///     The query must return a resultset.
        ///     All the authors in the resutset must have at least 1 title in business.
        /// Strategy:
        ///     All we want is to determine whether the select statement will return the resultset that we request or not.
        ///     One method is to verify if the each author in the resultset actually has a title in business.  So we'll be using 
        ///     another EQL query to verify the the results returned by first one.
        /// </summary>
        [TestMethod]
        public void TestSelectWhereJoined()
        {
            AuthorMeta author = new AuthorMeta("au");       // Create meta-info objects that will be used to refer to tables through entities.  Also specify an entity alias to be used in the context of this query.

            EQLDataAccess dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
            StatementScope scope = dataAccess.NewStatement; // Acquire a new StatementScope.

            AuthorCollection businessAuthors = new AuthorCollection();   // Create a new collection to fill into.

            // Select all authors who has at least 1 title in business
            scope.From(author)
               .Select(author.MappedColumns)
               .Where(
                   scope.From(author.AuthorTitles)
                       .WhereJoined(author.AuthorTitles.Right.Title.Right.type == "business")
                   .HasRows
               )
               .DebugDump("All the authors who has at least 1 title in business")
               .Into(businessAuthors);

            // Dump the result
            Debug.WriteLine(businessAuthors);

            // Decide pass or fail based on just the results returned.  Try not to use any temporarily valid assertions here.

            foreach (Author businessAuthor in businessAuthors)
            {
                dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
                scope = dataAccess.NewStatement;  // Acquire a new StatementScope.

                bool isBusinessAuthor = false;
                TitleAuthorMeta taut = new TitleAuthorMeta("taut");
                scope.FromNothing().Select(
                        scope.From(taut)
                            .WhereJoined(
                                taut.auId == businessAuthor.AuId
                                & taut.Title.Right.type == "business"
                             )
                            .HasRows
                    //.NewAlias("IsBusinessAuthor")     // may assign an alias to the column
                    )
                    .DebugDump("Determine if this author has any business titles.")
                    .IntoVar(ref isBusinessAuthor);

                Debug.WriteLine(isBusinessAuthor);

                Assert.IsTrue(isBusinessAuthor, String.Format("One of the loaded authors ({0}) is not a business author.", businessAuthor.AuId));
            }
        }

        [TestMethod]
        public void TestSelectWithDistinctAndNewAlias()
        {
            TitleMeta title = new TitleMeta("t");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("tau");
            AuthorMeta author = new AuthorMeta("au");

            // Prepare a sample expression to use in the following sample statements
            ExpressionString fullName = author.auLname + ", " + author.auFname;

            // Explicitly join tables on given conditions, filter the result and sort
            SelectStatement select = NewStatement.From(title)
                 .Distinct
                 .Select(author.MappedColumns).Select(fullName.NewAlias("fullName"))     // select an expression column and assign an alias to it
                 .InnerJoin(titleAuthor, title.titleId == titleAuthor.titleId)           // explicit inner join
                 .InnerJoin(author, titleAuthor.auId == author.auId)                     // explicit inner join
                 .Where(title.royalty > 0 & title.price > 50)                            // where condition. if you add another where, it will be anded with this one
                 .OrderBy(fullName)      // fullName was prepared above
                 .DebugDump("Select all authors who pay royalty and whose has a book more expensive than $50.")      // Check debug output to see what's dumped out.
             .Break; // We break the statement to continue later.  This is equivalent to casting to (SelectStatement) in this case.

            // Accessing to the prebuilt select statement and continue building it
            select.WhereClause.Where(title.price < 100).DebugDump("A new condition added to where");

            // add one more where condition.  this is anded with the previous one.

            //Assert.Inconclusive("Break and WhereClause not working");
        }

        [TestMethod]
        public void TestGivenParams()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = NewStatement;
            scope.DataAccess = this.NewDataAccess;
            SelectStatement select = scope.From(title)
                .SelectJoined(title.TitleAuthors.Right.MappedColumns)
                .Where(title.titleId == EQL.ParamString("titleID"))
                .DebugDump("Select a given title")
            .Break;

            // Then you can set the parameter value as follows:
            scope.ParamMap["titleID"] = mainTestTitleId;
            TitleAuthorCollection titleAuthorCol = new TitleAuthorCollection();
            select.Execute.Into(titleAuthorCol);
            foreach (TitleAuthor titleAuthorElem in titleAuthorCol)
                Assert.AreEqual(titleAuthorElem.TitleId, mainTestTitleId, "Element in the resultset does not belong to the the specified tileId. " + titleAuthorElem.ToString());
            //Assert.Inconclusive("Break not working");
        }

        [TestMethod]
        public void TestSimpleGroupByStatement()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = NewStatement;

            string pubId = "";
            int count = 0;
            scope.From(title).Select(title.pubId, EQL.Count(title.pubId))
            .Where(title.pubId == "1389")
            .GroupBy(title.pubId)
            .DebugDump("group by clause")
            .IntoVar(ref pubId, ref count);

            Assert.AreEqual("1389", pubId);
            Assert.AreEqual(6, count);
        }

        private static void CheckTitlesForGivenAuthor(TitleCollection titleCol, string authorId)
        {
            Assert.IsTrue(titleCol.Count == 2, "Resultset should return two item");
            foreach (Title titleElem in titleCol)
                Assert.IsTrue(titleElem.TitleId == "BU1032" || titleElem.TitleId == "BU2075", "The selected title in resultset not belong to author = " + authorId);
        }

        [TestMethod]
        public void TestImplicitSelectStatement()
        {
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("tau");
            StatementScope scope = NewStatement;
            TitleCollection titleCol = new TitleCollection();
            string authorId = "213-46-8915";
            scope.From(titleAuthor)
                .SelectJoined(titleAuthor.Title.Right.MappedColumns)        // SelectJoined will make sure all the relationships are added as left joins
                .Where(titleAuthor.auId == authorId)
                .DebugDump("The titles of a given author.").
                Into(titleCol);
            CheckTitlesForGivenAuthor(titleCol, authorId);
        }

        [TestMethod]
        public void Test2LevelImplicitSelectStatement()
        {
            AuthorMeta author = new AuthorMeta("au");
            StatementScope scope = NewStatement;
            TitleCollection titleCol = new TitleCollection();
            string authorId = "213-46-8915";
            scope.From(author)
                .SelectJoined(author.AuthorTitles.Right.Title.Right.MappedColumns)        // SelectJoined will make sure all the relationships are added as left joins
                .Where(author.auId == authorId)
                .DebugDump("The titles of a given author.").
                Into(titleCol);
            CheckTitlesForGivenAuthor(titleCol, authorId);
        }

        [TestMethod]
        public void TestSubQuerySelectStatement()
        {
            TitleMeta title = new TitleMeta("t");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("tau");
            StatementScope scope = NewStatement;
            TitleCollection titleCol = new TitleCollection();
            string authorId = "213-46-8915";
            ExpressionString expString = scope.From(titleAuthor)
                    .Select(titleAuthor.titleId)
                    .Where(titleAuthor.auId == authorId)
                    .SubQueryReturningString();
            scope.From(title).Select(title.MappedColumns).
            Where(title.titleId.In(expString)).DebugDump("The titles of a given author.").
            Into(titleCol);
            CheckTitlesForGivenAuthor(titleCol, authorId);
        }

        [TestMethod]
        public void TestSubQueryExistsSelectStatement()
        {
            TitleMeta title = new TitleMeta("t");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("tau");
            StatementScope scope = NewStatement;
            TitleCollection titleCol = new TitleCollection();
            string authorId = "213-46-8915";
            scope.From(title).Select(title.MappedColumns).
            Where(scope.From(titleAuthor)
                    .Select(titleAuthor.titleId)
                    .Where(titleAuthor.auId == authorId & titleAuthor.titleId == title.titleId).HasRows
            ).DebugDump("The titles of a given author.").
            Into(titleCol);
            CheckTitlesForGivenAuthor(titleCol, authorId);
        }

        [TestMethod]
        public void TestNotSubQueryInSelectStatement()
        {
            AuthorMeta author = new AuthorMeta("au");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("ta");
            StatementScope scope = NewStatement;
            AuthorCollection authorCol = new AuthorCollection();
            scope.From(author)
                .Select(author.MappedColumns)
                .Where(
                    !author.auId.In
                    (
                // SelectStatement's SubQueryReturning... methods convert a query into an expression so that you can use it in 
                // a where clause or any other expression.
                        scope.From(titleAuthor).Select(titleAuthor.auId).SubQueryReturningString()
                    )
                 )
                .DebugDump("Select all authors who have no titles.").Into(authorCol);
            foreach (Author elem in authorCol)
            {
                TitleAuthor temp = new TitleAuthor();
                bool result = NewStatement.From(titleAuthor).
                    Select(titleAuthor.MappedColumns).
                    Where(titleAuthor.auId == elem.AuId).Into(temp);
                Assert.AreEqual(false, result, "The query shouldn't find any row");
            }
        }

        [TestMethod]
        public void TestRowCountEqualToOne()
        {
            AuthorMeta author = new AuthorMeta("au");
            StatementScope scope = NewStatement;
            AuthorCollection authorCol = new AuthorCollection();
            scope.From(author)
                .SelectJoined(author.AuthorTitles.Right.Author.Right.MappedColumns)
                .Where(scope.From(author.AuthorTitles).RowCount == 1)
                .DebugDump("The authors who have a single title.")
                .Into(authorCol);
            foreach (Author elem in authorCol)
            {
                TitleAuthorMeta temp = new TitleAuthorMeta("ta");
                TitleAuthorCollection col = new TitleAuthorCollection();
                NewStatement.From(temp).Select(temp.MappedColumns).
                    Where(temp.auId == elem.AuId).Into(col);
                Assert.IsTrue(col.Count == 1, "The query should return only one title");
            }
        }

        [TestMethod]
        public void TestRowCountMoreThenZero()
        {
            AuthorMeta author = new AuthorMeta("au");
            StatementScope scope = NewStatement;
            AuthorCollection authorCol = new AuthorCollection();
            scope.From(author)
                .SelectJoined(author.AuthorTitles.Right.Author.Right.MappedColumns)
                .Where(scope.From(author.AuthorTitles).RowCount > 0)
                .DebugDump("The authors who have a one or many title.")
                .Into(authorCol);
            foreach (Author elem in authorCol)
            {
                TitleAuthorMeta temp = new TitleAuthorMeta("ta");
                TitleAuthorCollection col = new TitleAuthorCollection();
                NewStatement.From(temp).Select(temp.MappedColumns).
                    Where(temp.auId == elem.AuId).Into(col);
                Assert.IsTrue(col.Count > 0, "The query should return more then 0 title for author" + elem.AuId);
            }
        }

        [TestMethod]
        public void TestNotExistsSubQuerySelectStatement()
        {
            AuthorMeta author = new AuthorMeta("au");
            StatementScope scope = NewStatement;
            AuthorCollection authorCol = new AuthorCollection();

            scope = NewStatement;
            scope.From(author)
                .Select(author.MappedColumns)
                .Where(
                    scope.From(author.AuthorTitles)
                        .WhereJoined(author.AuthorTitles.Right.Title.Right.type == "business")
                    .HasNoRows)
                .DebugDump("All the authors who hasn't title in business")
                .Into(authorCol);

            foreach (Author elem in authorCol)
            {
                TitleMeta temp = new TitleMeta("t");
                TitleCollection col = new TitleCollection();
                NewStatement.From(temp)
                    .Select(temp.MappedColumns)
                    .WhereJoined(temp.TitleAuthors.Right.auId == elem.AuId)
                    .DebugDump("All tiltles for authors")
                    .Into(col);
                foreach (Title ta in col)
                    Assert.AreNotEqual<string>("business", ta.Type.Trim(), "The title.type value should be equal to business");
            }
        }

        [TestMethod]
        public void TestStringFieldInRow()
        {
            TitleMeta title = new TitleMeta("t");
            string titleType = "";
            NewStatement.From(title).Select(title.type).Where(title.titleId == mainTestTitleId)
            .IntoVar<string>(ref titleType);

            Assert.AreEqual<string>("psychology", titleType.Trim(), "Returned value of title type not equal to psychology");
        }

        [TestMethod]
        public void TestManyTablesFieldsInSingleRow()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = NewStatement;
            scope.From(title)
                .Select(title.MappedColumns).SelectJoined(title.Publisher.Right.MappedColumnsWithNewAlias("pub_"))      // add a prefix to all publisher columns to make them unique
                .InnerJoin(title.TitleAuthors)
                .DebugDump("All the titles and publishers");
            Assert.Inconclusive("No defined object for populating values");
        }

        [TestMethod]
        public void TestComplexQueryWithManyConditions()
        {
            TitleMeta title = new TitleMeta("t");
            TitleCollection titleCol = new TitleCollection();
            StatementScope scope = NewStatement;
            scope.From(title)
                .Select(title.MappedColumns)
                .WhereJoined(title.TitleAuthors.Right.Author.Right.city == "NEW YORK" | title.TitleAuthors.Right.royaltyper / 100 * 5 < 0.5
                    | scope.From(title.TitleAuthors).RowCount > 0
                    | scope.From(title.TitleAuthors).Select(title.TitleAuthors.Right.royaltyper.Avg()).SubQueryReturning<int>() > 5)
                .DebugDump("A complex query with many conditions")
                .Into(titleCol);
            Assert.IsTrue(titleCol.Count > 1, "At least should be a single row");
        }

        [TestMethod]
        public void TestComplexQueryWithSubQueries()
        {
            StatementScope scope = NewStatement;
            TitleMeta title = new TitleMeta("t");
            AuthorMeta author = new AuthorMeta("au");
            TitleMeta maTitles = new TitleMeta("MATitles");
            string firstAuthorsName = "";
            int count = -1;
            scope.From(
                // This is a subquery that is used in from
                // find authors whose publisher is from MA
                scope.From(title).Top(10)
                    .Select(title.titleId)
                    .Select(title.price)
                    .SelectJoined(title.TitleAuthors.Right.Author.Right.auLname.NewAlias("firstAuthorsName"))
                        .WhereJoined(title.Publisher.Right.state == "MA")       // select publisher from MA
                    .SubQueryOf(maTitles)
                // End of subquery used in from
                )
                .Select
                (
                // This is a subquery used in select
                // Select whose authors are also from MA
                    scope.From(author)
                    .Where(author.state == "MA")
                    .RowCount.NewAlias("NumAuthorsMA")
                // End of subquery used in select
                )
                .Select(maTitles.StringMember("firstAuthorsName"))
                .DebugDump("Complex query with subqueries")
                .IntoVar<int, string>(ref count, ref firstAuthorsName);
            Assert.IsTrue(count != -1, "Count of authors in MA not populated");
            Assert.IsTrue(firstAuthorsName.Length != 0, "The authors name not populated");
            Assert.Inconclusive("Cannot populate recordset");
        }

        [TestMethod]
        public void TestSameExpressionInMultiplePlaces()
        {
            AuthorMeta author = new AuthorMeta("au");
            ExpressionString fullName = author.auLname + ", " + author.auFname;
            StatementScope scope = NewStatement;
            string authorsName = "";
            scope.From(author).Select(fullName)
                .Where(fullName.StartsWith("Ringer"))
                .OrderBy(fullName.Descending)
                .DebugDump("Using same expression in multiple places in a query")
                .IntoVar<string>(ref authorsName);
            Assert.IsTrue(authorsName.StartsWith("Ringer"), "Value not started with Ringer");
            //Assert.Inconclusive("Like and order by clauses not properly populated");
            //.Break;

        }

        [TestMethod]
        public void TestSelectedRowCountAvgMaxMin()
        {
            int count = 0;
            double auOrdAvg = 0;
            int maxRoyalty = 0;
            double minYtdSales = 0;
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = NewStatement;
            scope.From(title)
                //.Distinct
                .SelectJoined(EQL.RowCount(), title.royalty.Max(), title.price.Avg(), title.ytdSales.Min())
                .WhereJoined(title.TitleAuthors.Right.Author.Right.state == "CA")
                .DebugDump("Number of titles whose authors are from CA, max royalty paid, and the average price")
                .IntoVar(ref count, ref maxRoyalty, ref auOrdAvg, ref minYtdSales);
            Assert.IsTrue(count > 0 && auOrdAvg > 0 && maxRoyalty > 0 && minYtdSales > 0, "The values not properly populated");
        }

        [TestMethod]
        public void TestSubStringFunction()
        {
            TitleMeta title = new TitleMeta("t");

            string substring = "";
            NewStatement.From(title)
                .Select(
                    title.title.Substring(1, 2)
                ).Where(title.titleId == mainTestTitleId)
                .DebugDump("The substring")
                .IntoVar(ref substring);
            Assert.AreEqual("Is", substring);
        }

        [TestMethod]
        public void TestCharIndexFunction()
        {
            TitleMeta title = new TitleMeta("t");

            int charindex = 0;
            NewStatement.From(title)
                .Select(
                    title.type.CharIndex("o", 7)
                ).Where(title.titleId == mainTestTitleId)
                .DebugDump("The substring")
                .IntoVar(ref charindex);
            Assert.AreEqual(8, charindex);
        }

        [TestMethod]
        public void TestStringFunctions()
        {
            TitleMeta title = new TitleMeta("t");

            string trim = "";
            string upper = "";
            string lower = "";
            string rtrim = "";
            NewStatement.From(title)
                .Select(
                    title.type.Trim(),
                    title.type.Trim().Upper(),
                    title.type.Trim().Upper().Lower(),
                    title.type.RTrim()
                ).Where(title.titleId == mainTestTitleId)
                .DebugDump("The upper, lower, rtrim, trim functions")
                .IntoVar(ref trim, ref upper, ref lower, ref rtrim);

            Assert.AreEqual("psychology", trim);
            Assert.AreEqual("PSYCHOLOGY", upper);
            Assert.AreEqual("psychology", lower);
            Assert.AreEqual("psychology", rtrim);

            string ltrim = "";
            string left = "";
            string soundexright = "";
            string replace = "";
            ExpressionString whiteSpace = " ";
            NewStatement.From(title)
                .Select(
                    (whiteSpace + title.titleId).LTrim(),
                    title.type.Left(4),
                    title.type.Soundex.Right(3),
                    title.type.Trim().Replace("y", "i")
                ).Where(title.titleId == mainTestTitleId)
                .DebugDump("The upper, lower, rtrim, trim functions")
                .IntoVar(ref ltrim, ref left, ref soundexright, ref replace);

            Assert.AreEqual(mainTestTitleId, ltrim);
            Assert.AreEqual("psyc", left);
            Assert.AreEqual("224", soundexright);
            Assert.AreEqual("psichologi", replace);
        }

        [TestMethod]
        public void TestUnionBaseFunctionalityStatement()
        {
            TitleMeta title = new TitleMeta("t");
            TitleCollection titleCol = new TitleCollection();
            StatementScope scope = this.NewStatement;

            scope.Union(
                scope.From(title).Select(title.MappedColumns).Where(title.titleId == mainTestTitleId),
                scope.From(title).Select(title.MappedColumns).Where(title.titleId == mainTestTitleId)
                ).DebugDump("The Union should return recordset with single row")
                .Into(titleCol);
            Assert.AreEqual(1, titleCol.Count, "Returning rows count must be equal to 1");

            scope = this.NewStatement;
            titleCol = new TitleCollection();
            scope.UnionAll(
                scope.From(title).Select(title.MappedColumns).Where(title.titleId == mainTestTitleId),
                scope.From(title).Select(title.MappedColumns).Where(title.titleId == mainTestTitleId)
                ).DebugDump("The Union should return recordset with 2 rows")
                .Into(titleCol);
            Assert.AreEqual(2, titleCol.Count, "Returning rows count must be equal to 2");

            scope = this.NewStatement;
            titleCol = new TitleCollection();
            scope.Union(
            scope.From(title).Select(title.MappedColumns).Where(title.titleId == mainTestTitleId),
            scope.UnionAll(
                scope.From(title).Select(title.MappedColumns).Where(title.titleId == mainTestTitleId),
                scope.From(title).Select(title.MappedColumns).Where(title.titleId == mainTestTitleId)
                ))
                .DebugDump("The Union should return recordset with 1 row")
                .Into(titleCol);
            Assert.AreEqual(1, titleCol.Count, "Returning rows count must be equal to 1");
        }

        [TestMethod]
        public void TestUnionStatement()
        {
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("taut");
            TitleAuthorMeta mySubquery = new TitleAuthorMeta("mySub");

            StatementScope scope = this.NewStatement;

            // nested union
            TitleAuthorCollection titleAuthorCol = new TitleAuthorCollection();
            scope.Union(
                    scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PS2091"),
                    scope.UnionAll(
                        scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "BU1032"),
                        scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PC8888")
                        )
                ).DebugDump("unioned")
                .Into(titleAuthorCol);
            Assert.AreEqual(6, titleAuthorCol.Count, "Returning rows count for nested union must be equal to 6");

            // union in subquery
            scope = this.NewStatement;
            titleAuthorCol = new TitleAuthorCollection();
            scope.From(
                scope.Union(
                        scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PS2091"),
                        scope.UnionAll(
                            scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "BU1032"),
                            scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PC8888")
                            )
                    ).DebugDump("unioned")
                    .SubQueryOf(mySubquery)
            ).DebugDump("select from union")
            .Into(titleAuthorCol);
            Assert.AreEqual(6, titleAuthorCol.Count, "Returning rows count for union in subquery must be equal to 6");

            // union using + operator
            scope = this.NewStatement;
            titleAuthorCol = new TitleAuthorCollection();
            UnionStatement unioned = scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PS2091").Break
                + scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PC8888").Break;
            unioned.DebugDump("Unioned");
            unioned.Execute.Into(titleAuthorCol);
            Assert.AreEqual(4, titleAuthorCol.Count, "Returning rows count for union using + operator must be equal to 4");

            // union using + operator in one step
            scope = this.NewStatement;

            titleAuthorCol = new TitleAuthorCollection();
            (
                scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PS2091").Break
                + scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PC8888").Break
            ).DebugDump("Unioned")
             .Execute
             .Into(titleAuthorCol);
            Assert.AreEqual(4, titleAuthorCol.Count, "Returning rows count for union using + operator in one step must be equal to 4");
        }

        [TestMethod]
        public void TestUpdateStatement()
        {
            TitleMeta title = new TitleMeta("t");

            string notes = "Muckraking reporting on the world's largest computer hardware and software manufacturers.";
            int ytdSales = 4095;

            int affectedRows =
                NewStatement.Update(title)
                    .Set(title.notes, "title.notes").Set(title.ytdSales, 4000)
                        .Where(title.titleId == "PC8888")
                        .DebugDump("Update title")
                        .Invoke();

            Assert.AreEqual(1, affectedRows, "Affected rows count ufter update statement by primary key must be equal to one");

            Title titleObj = new Title();

            NewStatement.From(title).Select(title.MappedColumns).Where(title.titleId == "PC8888")
                .Into(titleObj);

            NewStatement.Update(title)
                .Set(title.notes, notes).Set(title.ytdSales, ytdSales)
                    .Where(title.titleId == "PC8888")
                    .Invoke();

            Assert.IsTrue(titleObj.Notes != notes && titleObj.YtdSales != ytdSales, "The update statement not properly executed");
        }

        [TestMethod]
        public void TestInsertDeleteStatement()
        {
            TitleMeta title = new TitleMeta("t");
            PublisherMeta pub = new PublisherMeta("p");

            string titleId = "TSTINS";

            int affectedRows =
                NewStatement.Insert(title)
                    .Set(title.titleId, titleId)
                    .Set(title.title, "Tested title")
                    .Set(title.type, "UNDEFINED")
                    .Set(title.pubId, "0736")
                    .Set(title.pubdate, DateTime.Today)
                    .DebugDump("Insert title")
                    .Invoke();

            Debug.WriteLine(affectedRows);

            Title titleObj = new Title();

            NewStatement.From(title).Select(title.MappedColumns).Where(title.titleId == titleId)
                .Into(titleObj);

            Assert.AreEqual(titleId, titleObj.TitleId, "Insert statement not executed");
            Debug.WriteLine(titleObj);

            //affectedRows =
            //    NewStatement.Delete(title).InnerJoin(pub, title.pubId == pub.pubId)
            //    .Where(title.titleId == titleObj.TitleId & pub.pubId == "0736").DebugDump("Delete title")
            //    .Invoke();
            affectedRows =
                NewStatement.Delete(title)
                .Where(title.titleId == titleObj.TitleId).DebugDump("Delete title")
                .Invoke();

            Debug.WriteLine(affectedRows);

            titleObj = new Title();

            NewStatement.From(title).Select(title.MappedColumns).Where(title.titleId == titleId)
                .Into(titleObj);

            Assert.IsTrue(string.IsNullOrEmpty(titleObj.TitleId), "Delete statement not executed");
        }


        [TestMethod]
        public void TestLoadByFilter()
        {
            // Define a title meta-info object to refer in expressions.
            TitleMeta TITLE = new TitleMeta("t");       // "t" is the alias to be used in SQL

            Title title = new Title();
            // Passing a filter to an object load method:
            title.LoadByFilter(TITLE.titleId == mainTestTitleId);
            Debug.WriteLine(title);
            Assert.AreEqual(title.TitleId, mainTestTitleId, "Query returned a different record.");

            TitleCollection titles = new TitleCollection();
            // Passing both a filter and order by terms:
            titles.LoadByFilter(TITLE.title.StartsWith("t") | TITLE.type.RTrim() == "business", TITLE.pubdate.Descending);
            Debug.WriteLine(titles);
            DateTime temp = DateTime.Today.AddDays(1);
            foreach (Title item in titles)
            {
                Assert.IsTrue(item.TitleText.ToUpper().StartsWith("T") || item.Type.TrimEnd(new char[] { ' ' }) == "business", "Items not properly filtered");
                Assert.IsTrue(item.Pubdate <= temp, "The recordset didn't sort by descending");
                temp = item.Pubdate;
            }
        }

        [TestMethod]
        public void TestBatchExecutionAndFillingMultipleObjects()
        {
            StatementScope scope = this.NewStatement;
            TitleMeta TITLE = new TitleMeta("t");       // "t" is the alias to be used in SQL

            List<DateTime> dates = new List<DateTime>();
            Title title1 = new Title();
            Title title2 = new Title();
            Title title3 = new Title();
            TitleCollection titles = new TitleCollection();

            scope.Batch(
                // Acquire current time and fill it into given list
                                scope.Select(EQL.Now).BatchResult.IntoList(dates),

                                // Load the first version of title
                                scope.From(TITLE).Select(TITLE.SelectableColumns)
                                    .Where(TITLE.titleId == mainTestTitleId)
                                    .BatchResult.Into(title1),

                                // Update it
                                scope.Update(TITLE).Set(TITLE.title, TITLE.title + " updated").Where(TITLE.titleId == mainTestTitleId),

                                // Load the updated version of title
                                scope.From(TITLE).Select(TITLE.MappedColumns)
                                    .Where(TITLE.titleId == mainTestTitleId)
                                    .BatchResult.Into(title2),

                                // Update it back to original
                                scope.Update(TITLE).Set(TITLE.title, TITLE.title.RCut(8)).Where(TITLE.titleId == mainTestTitleId),

                                // Load the updated back object into a third instance
                                scope.From(TITLE).Select(TITLE.MappedColumns)
                                    .Where(TITLE.titleId == mainTestTitleId)
                                    .BatchResult.Into(title3),

                                // select all titles cheaper than $5.
                                scope.From(TITLE).Select(TITLE.MappedColumns)
                                    .Where(TITLE.price < 5)
                                    .BatchResult.Into(titles),

                                // add all titles more expensive than $15 into the same collection.
                                scope.From(TITLE).Select(TITLE.MappedColumns)
                                    .Where(TITLE.price > 15)
                                    .BatchResult.Into(titles)

                            )
                            .DebugDump("Batch statement")
                            .InvokeBatch();                    // Execute the entire statement in one roundtrip and fill into the specified target objects

            Assert.IsTrue(dates.Count == 1, "In the recordset should be one row with the current date");

            Assert.IsTrue(title1.TitleId == title2.TitleId, "The first Title id must be equal to the second");
            Assert.IsTrue(title1.TitleText != title2.TitleText, "The First Title name must be not equal to the second");

            Assert.IsTrue(title2.TitleId == title3.TitleId, "The Second Title id must be equal to the third");
            Assert.IsTrue(title2.TitleText != title3.TitleText, "The Second Title name must be not equal to the third");

            Assert.IsTrue(title1.TitleText == title3.TitleText, "The First Title name must be equal to the third");

            foreach (Title item in titles)
            {
                Assert.IsTrue(item.Price < 5 || item.Price > 15, "The title price in the collection must be between 5 and 15");
            }
        }



        [TestMethod]
        public void SampleReadEach()
        {
            TitleMeta TITLE = new TitleMeta("t");
            this.NewStatement.From(TITLE).ReadEach<Title>(false,
                delegate(EQLReaderContext<Title> context)
                {
                    Debug.WriteLine(context.RecordIndex);
                    Debug.WriteLine(context.Entity);
                    Debug.WriteLine(context.Reader.GetString(0));
                });
        }

    }

}
