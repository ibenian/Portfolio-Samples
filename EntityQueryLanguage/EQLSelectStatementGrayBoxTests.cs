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
using NS.EQL.Model;

using PubsObjectModel;
using PubsObjectModel.Meta;
using System.Text.RegularExpressions;

namespace EQLTest.TestClasses
{
    /// <summary>
    /// Implement isolated unit tests here.  These tests must follow graybox approach:
    ///     1. Build an EQL statement, or an expression.
    ///     2. Use one of the following ways to look into the intermediary results to decide:
    ///         check the expression tree or statement tree to decide whether the operation is successful.
    ///         check the rendered query to decide whether the operation is successful.
    ///     3. These tests must be written against well-defined unit-level behavioral requirements.
    /// 
    /// This approach is useful when it is not possible or feasable to do a test just by looking at the returned query result.
    /// It is also useful for checking some behavioral requirements that are too fine grained and can be isolated from its execution behavior.
    /// There are certain features that the EQL expression model and statement model should implement automatically.  These should have
    /// such unit-tests that act as a guard against regression as EQL evolves.
    /// 
    /// These tests require some knowledge of the EQL model because we look into the intermediary results rather than the result of the executed query.
    /// Intermidiary results are:  
    ///     1. The rendered string for an expression or statement or a part of a statement.
    ///     2. The EQL object tree (expression tree or statement object tree).
    /// </summary>
    [TestClass]
    public class EQLSelectStatementGrayBoxTests : BaseTestWithEQLDataAccess
    {
        private const string mainTestTitleId = "PS2091";
        /// <summary>
        /// The Test:
        ///     Build an expression and try to cast the the boolean expression into boolean value to see if it is properly handled.
        ///     A boolean expression is convered to a boolean value by wrapping within a case block as follows:
        ///         Case When ... Then 1 Else 0 End
        /// Expectations:
        ///     The object model must be constructed properly, or the rendered string must contain the expected format.
        /// Strategy:
        ///     Checking the rendered string is much harder but would definetly be useful.  Some helpers may be required to do such checks.
        ///     Here we'll just check the object model.
        /// </summary>
        [TestMethod]
        public void CastingBooleanExpressionToBooleanValue()
        {
            TitleMeta title = new TitleMeta("t");

            // Create a boolean expreesion that compares two terms.
            BooleanExpression boolExp = title.titleId == "PS2091";

            // Now convert it into a boolean value.
            ExpressionBoolValue boolValExp = boolExp.AsBool();

            // Dump the expressions.
            // Expressions that are not in an EQL statement will be dumped out by debug-time renderer which is closer to C# syntax.
            // For example boolean values are rendered as True and False.
            Debug.WriteLine(String.Format("Expression '{0}' was converted to bool value as '{1}'", boolExp, boolValExp));

            CheckIfWrapedByCaseBlock(boolValExp);

            // Now let's select a boolean expression in an EQL statement and check the same.

            EQLDataAccess dataAccess = this.NewDataAccess;     // Acquire DataAccess context.
            StatementScope scope = dataAccess.NewStatement; // Acquire a new StatementScope.

            SelectStatement statement = (SelectStatement)scope.From(title).Select(title.titleId == "PS2091")
                .DebugDump("Select a boolean expression");

            EQLSelectColumn selCol = statement.EQLSelectStatement.SelectColumns[0];

            boolValExp = selCol.Expression as ExpressionBoolValue;      // must be converted to a boolean value since we used in Select(...)

            CheckIfWrapedByCaseBlock(boolValExp);

        }

        /// <summary>
        /// Checks if the given bool value expression actually contains an object tree that corresponds to Case When ... Then 1 Else 0
        /// </summary>
        /// <param name="boolValExp"></param>
        private static void CheckIfWrapedByCaseBlock(ExpressionBoolValue boolValExp)
        {
            EQLCaseTerm caseTerm = boolValExp.Term as EQLCaseTerm;
            Assert.IsTrue(caseTerm != null, "The boolean expression is not wrapped in a case term!");



            // 1-) Either check the rendered intermediary result:


            // Use regular expression Regex.IsMatch or a simple string search
            Assert.IsTrue(boolValExp.ToString().ToUpper().IndexOf("THEN TRUE ELSE FALSE END") > 0, "The term is not properly wrapped within a Case block!");


            // 2-) Or check the object tree created:

            // Checking the rendered string is not the only option.
            // We can also recurse into the expression tree to check the expected objects.
            // Following is an example of how the expression tree can be checked.

            List<EQLWhenTerm> whenTerms = ExpressionTreeHelper.FindSubTerms<EQLWhenTerm>(caseTerm, true);
            Assert.IsTrue(whenTerms.Count == 2, "2 when terms are exptected for when and else!");

            EQLConstantTerm constTerm = null;

            // check when .. then 1
            constTerm = whenTerms[0].ThenExpression.Term as EQLConstantTerm;
            Assert.IsTrue(constTerm != null, "When term must be a bool expression");
            Assert.IsTrue(constTerm.Value is bool, "When term must be a boolean");
            Assert.IsTrue((bool)constTerm.Value == true, "When term must be True");

            // check else 0

            Assert.IsTrue(whenTerms[1].WhenExpression == null, "The second term of Case must be a Then term");
            constTerm = whenTerms[1].ThenExpression.Term as EQLConstantTerm;
            Assert.IsTrue(constTerm != null, "Else term must be a bool expression");
            Assert.IsTrue(constTerm.Value is bool, "Else term must be a boolean");
            Assert.IsTrue((bool)constTerm.Value == false, "When term must be False");

        }

        private static string RemoveUnnecessarySymbols(string sqlStatement)
        {
            string[] unnecessarySymbols = { Environment.NewLine, "\n", "\r", "\t", "    ", "   ", "  " };
            string returningSql = sqlStatement;
            foreach (string symbols in unnecessarySymbols)
            {
                returningSql = returningSql.Replace(symbols, " ");
            }
            return returningSql;
        }

        private const string selectSingleRowByPrimaryKey =
            "SELECT t.[title_id], t.[title], t.[type], t.[pub_id], t.[price], t.[advance], t.[royalty], t.[ytd_sales], t.[notes], t.[pubdate] FROM [titles] t WHERE (t.[title_id]=@param1)";
        [TestMethod]
        public void TestSelectSingleRowByPrimaryKey()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = this.NewStatement;
            Title titleObject = new Title();

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(title.MappedColumns)
                .Where(title.titleId == mainTestTitleId)
                .DebugDump("Load a title.");

            Assert.AreEqual(selectSingleRowByPrimaryKey, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string selectDistinctLike =
            "SELECT DISTINCT t.[pub_id] FROM [titles] t WHERE (t.[title_id] LIKE (@param1+'%'))";
        [TestMethod]
        public void TestSelectDistinctLike()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = this.NewStatement;

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Distinct
                .Select(title.pubId)
                .Where(title.titleId.StartsWith(mainTestTitleId))
                .DebugDump("Select Distinct Like");

            Assert.AreEqual(selectDistinctLike, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string selectOrderByAndExpression =
            "SELECT a.[au_id], (a.[au_lname]+', '+a.[au_fname]) " +
                "FROM [authors] a " +
                "WHERE (a.[state]<>'CA') " +
                "ORDER BY a.[au_id] DESC, (a.[au_lname]+', '+a.[au_fname]) ASC";
        [TestMethod]
        public void TestSelectOrderByAndExpression()
        {
            AuthorMeta author = new AuthorMeta("a");
            StatementScope scope = this.NewStatement;
            ExpressionString fullName = author.auLname + ", " + author.auFname;

            SelectStatement statement = (SelectStatement)scope.From(author)
                .Select(author.auId, fullName)
                .Where(author.state != "CA")
                .OrderBy(author.auId.Descending, fullName)
                .DebugDump("Select Order By");

            Assert.AreEqual(selectOrderByAndExpression, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string selectCountSumMinMaxGroupByHaving =
            "SELECT Count(t.[title_id]), Sum(t.[price]), Min(t.[price]), Max(t.[price]) " +
            "FROM [titles] t " +
            "GROUP BY t.[type], t.[pub_id] " +
            "HAVING ((t.[pub_id]=@param1) AND Sum(t.[price]) IS NOT NULL)";
        [TestMethod]
        public void TestSelectCountSumMinMaxGroupByHaving()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = this.NewStatement;

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(EQL.Count(title.titleId), title.price.Sum(), title.price.Min(), title.price.Max())
                .GroupBy(title.type, title.pubId)
                .Having(title.pubId == "0877" & title.price.Sum().IsNotNull)
                .DebugDump("Select Count Sum Min Max GroupBy Having");

            Assert.AreEqual(selectCountSumMinMaxGroupByHaving, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string selectInnerJoin =
            "SELECT t.[pub_id] " +
            "FROM [titles] t " +
            "INNER JOIN [titleauthor] ta ON (t.[title_id]=ta.[title_id]) " +
            "INNER JOIN [authors] a ON (ta.[au_id]=a.[au_id])";
        [TestMethod]
        public void TestSelectInnerJoin()
        {
            TitleMeta title = new TitleMeta("t");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("ta");
            AuthorMeta author = new AuthorMeta("a");

            StatementScope scope = this.NewStatement;

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(title.pubId)
                .InnerJoin(titleAuthor, title.titleId == titleAuthor.titleId)
                .InnerJoin(author, titleAuthor.auId == author.auId)
                .DebugDump("Inner join");
            Assert.AreEqual(selectInnerJoin, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string selectLeftJoin =
            "SELECT t.[pub_id] " +
            "FROM [titles] t " +
            "LEFT JOIN [titleauthor] ta ON (t.[title_id]=ta.[title_id])";
        [TestMethod]
        public void TestSelectLeftJoin()
        {
            TitleMeta title = new TitleMeta("t");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("ta");
            StatementScope scope = this.NewStatement;

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(title.pubId)
                .LeftJoin(titleAuthor, title.titleId == titleAuthor.titleId)
                .DebugDump("Left join");
            Assert.AreEqual(selectLeftJoin, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string selectRightJoin =
            "SELECT t.[pub_id] " +
            "FROM [titleauthor] ta " +
            "RIGHT JOIN [titles] t ON (t.[title_id]=ta.[title_id])";
        [TestMethod]
        public void TestSelectRightJoin()
        {
            TitleMeta title = new TitleMeta("t");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("ta");
            StatementScope scope = this.NewStatement;

            SelectStatement statement = (SelectStatement)scope.From(titleAuthor)
                .Select(title.pubId)
                .RightJoin(title, title.titleId == titleAuthor.titleId)
                .DebugDump("Right join");
            Assert.AreEqual(selectRightJoin, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string selectFullJoin =
            "SELECT t.[pub_id], p.[pub_id] " +
            "FROM [titles] t " +
            "FULL JOIN [publishers] p ON (t.[pub_id]=p.[pub_id])";
        [TestMethod]
        public void TestSelectFullJoin()
        {
            TitleMeta title = new TitleMeta("t");
            PublisherMeta publisher = new PublisherMeta("p");
            StatementScope scope = this.NewStatement;

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(title.pubId, publisher.pubId)
                .FullJoin(publisher, title.pubId == publisher.pubId)
                .DebugDump("Full join");
            Assert.AreEqual(selectFullJoin, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string unionStatementWithSubQuery =
            "SELECT * FROM ( " +
            "SELECT tTitleAuthors.[au_id], tTitleAuthors.[title_id], tTitleAuthors.[au_ord], tTitleAuthors.[royaltyper] " +
            "FROM [titles] t " +
            "LEFT JOIN [titleauthor] tTitleAuthors ON (t.[title_id]=tTitleAuthors.[title_id]) " +
            "WHERE (t.[title_id]=@titleID) " +

            "UNION " +
            "SELECT ta.[au_id], ta.[title_id], ta.[au_ord], ta.[royaltyper] " +
            "FROM [titleauthor] ta ) taUnion " +

            "WHERE ((taUnion.[au_id]<>@auID) " +
            "AND (taUnion.[title_id]=@auOrd))";
        [TestMethod]
        public void TestUnionStatementWithSubQuery()
        {
            TitleMeta title = new TitleMeta("t");
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("ta");
            TitleAuthorMeta mySubquery = new TitleAuthorMeta("taUnion");
            StatementScope scope = this.NewStatement;

            SelectStatement statement =
                scope.From(
                scope.Union(
                    scope.From(title)
                        .SelectJoined(title.TitleAuthors.Right.MappedColumns)
                        .Where(title.titleId == EQL.ParamString("titleID"))
                        .Break,
                    scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Break)
                    .SubQueryOf(mySubquery))
                 .Where(mySubquery.auId != EQL.ParamString("auID"))
                 .DebugDump("Union with subquery").Break;

            scope.ParamMap["titleID"] = mainTestTitleId;
            scope.ParamMap["auID"] = "172-32-1176";

            statement.WhereClause.Where(mySubquery.titleId == EQL.ParamString("auOrd"));

            scope.ParamMap["auOrd"] = 1;

            Assert.AreEqual(unionStatementWithSubQuery, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string unionAllStatement =
            "SELECT ta.[au_id], ta.[title_id], ta.[au_ord], ta.[royaltyper] " +
            "FROM [titleauthor] ta " +
            "WHERE (ta.[title_id]=@param1) " +
            "UNION ALL " +
            "SELECT ta.[au_id], ta.[title_id], ta.[au_ord], ta.[royaltyper] " +
            "FROM [titleauthor] ta " +
            "WHERE (ta.[title_id]=@param2) " +
            "UNION ALL " +
            "SELECT ta.[au_id], ta.[title_id], ta.[au_ord], ta.[royaltyper] " +
            "FROM [titleauthor] ta " +
            "WHERE (ta.[title_id]=@param3) " +
            "UNION ALL " +
            "SELECT ta.[au_id], ta.[title_id], ta.[au_ord], ta.[royaltyper] " +
            "FROM [titleauthor] ta";

        [TestMethod]
        public void TestUnionAllStatement()
        {
            TitleAuthorMeta titleAuthor = new TitleAuthorMeta("ta");
            StatementScope scope = this.NewStatement;

            UnionStatement union = (UnionStatement)scope.UnionAll(
                        scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "BU1032"),
                        scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "PC8888")
                        )
                 .DebugDump("Union two resultsets").Break;
            union += scope.From(titleAuthor).Select(titleAuthor.MappedColumns).Where(titleAuthor.titleId == "BU1032").Break;
            union.UnionClause.Union(scope.From(titleAuthor).Select(titleAuthor.MappedColumns))
                .DebugDump("Union four resultsets");

            Assert.AreEqual(unionAllStatement, RemoveUnnecessarySymbols(union.ToString()));
        }

        private const string stringFunctions =
            "SELECT LTrim(t.[title]), " +
            "RTrim(t.[title]), " +
            "LTrim(RTrim(t.[title])), " +
            "Upper(t.[title]), " +
            "Lower(t.[title]), " +
            "Soundex(t.[title]), " +
            "Replace(t.[title], ' ', '_'), " +
            "(t.[title_id]+', '+t.[title]), " +
            "Left(t.[title_id]+', '+t.[title], 4), " +
            "Right(t.[title_id]+', '+t.[title], 4), " +
            "Substring(t.[title], 1, 3) " +
            "FROM [titles] t " +
            "WHERE (t.[title_id]=@param1)";
        [TestMethod]
        public void TestStringFunctions()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = this.NewStatement;

            ExpressionString fullName = title.titleId + ", " + title.title;

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(
                    title.title.LTrim(),                        // LTrim(title)
                    title.title.RTrim(),                        // RTrim(title)
                    title.title.Trim(),                         // LTrim(RTrim(title))
                    title.title.Upper(),                        // Upper(title)
                    title.title.Lower(),                        // Lower(title)
                    title.title.Soundex,                        // Soundex(title)
                    title.title.Replace(" ", "_"),              // Replace(title, '.', '_')
                    fullName,                                   // previously prepared expression auLname + ', ' + auFname
                    fullName.Left(4),                           // Left(auLname + ', ' + auFname, 4)
                    fullName.Right(4),                          // Right(auLname + ', ' + auFname, 4)
                    title.title.Substring(1, 3)                 // Substring(title, 1, 3) ? error
                )
                .Where(title.titleId == mainTestTitleId)
                .DebugDump("String functions");

            Assert.AreEqual(stringFunctions, RemoveUnnecessarySymbols(statement.ToString()));
            //Should be CharIndex(' ', title, 3) instead of CharIndex(title, ' ', 3)
            //Should be Substring(title, 1, 3) instead of Substring(1, 3)
        }

        private const string selectExists =
            "SELECT t.[title_id] FROM [titles] t " +
            "WHERE (NOT EXISTS ( " +
            "SELECT * FROM [titleauthor] tTitleAuthors " +
            "WHERE ((t.[title_id]=tTitleAuthors.[title_id]) " +
            "AND (t.[title_id] LIKE ('%'+'BU'+'%'))) ) " +
            "AND EXISTS ( " +
            "SELECT p.[pub_id] FROM [publishers] p " +
            "WHERE (p.[pub_id]=t.[pub_id]) ))";
        [TestMethod]
        public void TestSelectExists()
        {
            TitleMeta title = new TitleMeta("t");
            PublisherMeta publisher = new PublisherMeta("p");
            StatementScope scope = this.NewStatement;
            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(title.titleId)
                .Where(scope.From(title.TitleAuthors)
                      .Where(title.titleId.Contains("BU"))
                      .HasNoRows &
                        scope.From(publisher)
                        .Select(publisher.pubId)
                        .Where(publisher.pubId == title.pubId)
                        .HasRows).DebugDump("Select exists");

            Assert.AreEqual(selectExists, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string insertStatement =
            "INSERT INTO [titles] " +
            "([title_id], [title], [type], [pub_id]) " +
            "VALUES (@param1, @param2, @param3, @param4)";
        [TestMethod]
        public void TestInsertStatement()
        {
            TitleMeta title = new TitleMeta("t");
            InsertStatement statement = (InsertStatement)NewStatement.Insert(title)
                .Set(title.titleId, "TSTINS")
                .Set(title.title, "Tested title")
                .Set(title.type, "UNDEFINED")
                .Set(title.pubId, "0736")
                //.Set(title.pubdate, EQL.ParamString("Pubdate"))
                .DebugDump("Insert title");
            Assert.AreEqual(insertStatement, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string updateStatement =
            "UPDATE [titles] SET [notes]=@param1, [ytd_sales]=@param2 " +
            "WHERE ([pub_id] IN ( ( SELECT p.[pub_id] FROM [publishers] p )))";
        [TestMethod]
        public void TestUpdateStatement()
        {
            TitleMeta title = new TitleMeta("t");
            PublisherMeta publisher = new PublisherMeta("p");
            StatementScope scope = this.NewStatement;

            UpdateStatement statement = (UpdateStatement)scope.Update(title)
                .Set(title.notes, "notes")
                .Set(title.ytdSales, 4000)
                .Where(title.pubId.In(
                    scope.From(publisher)
                    .Select(publisher.pubId)
                    .SubQueryReturningString()))
                    .DebugDump("Update statement");
            Assert.AreEqual(updateStatement, RemoveUnnecessarySymbols(statement.ToString()));
            //Should be @param2 instead of 4000;
        }

        private const string deleteStatement =
            "DELETE FROM [titles] WHERE ([titles].[pub_id]<>@param1)";
        [TestMethod]
        public void TestDeleteStatement()
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = this.NewStatement;

            DeleteStatement statement = (DeleteStatement)scope.Delete(title)
                .Where(title.pubId != "9999")
                .DebugDump("Delete statement");
            Assert.AreEqual(deleteStatement, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private void CheckEnableIfMethod(bool enableEQL, string assert)
        {
            TitleMeta title = new TitleMeta("t");
            StatementScope scope = this.NewStatement;
            ExpressionString fullName = title.titleId + ", " + title.title;

            SelectStatement statement = (SelectStatement)scope.From(title)
                .Select(
                    title.title,
                    title.title.LTrim().EnableIf(enableEQL),    // LTrim(title)
                    title.titleId.EnableIf(enableEQL),          // RTrim(title)
                    fullName.EnableIf(enableEQL)                // previously prepared expression auLname + ', ' + auFname
                )
                .Where(title.titleId.EnableIf(enableEQL) == mainTestTitleId & title.pubId == "0736"
                    & EQL.EnableIf(enableEQL, title.royalty > 0 & title.price > 50))
                .DebugDump("Enableif( " + enableEQL.ToString() + " ) usage");
            Assert.AreEqual(assert, RemoveUnnecessarySymbols(statement.ToString()));
        }

        private const string enableIfTrue =
            "SELECT t.[title], LTrim(t.[title]), t.[title_id], (t.[title_id]+', '+t.[title]) " +
            "FROM [titles] t " +
            "WHERE (((t.[title_id]=@param1) AND (t.[pub_id]=@param2)) AND ((t.[royalty]>0) AND (t.[price]>50)))";
        private const string enableIfFalse =
            "SELECT t.[title] " +
            "FROM [titles] t " +
            "WHERE (t.[pub_id]=@param1)";
        [TestMethod]
        public void TestEnableIfMethod()
        {
            CheckEnableIfMethod(true, enableIfTrue);
            CheckEnableIfMethod(false, enableIfFalse);
        }

        private const string updateStatementWithParamSource =
        "UPDATE [titles] SET [titles].[title]=([titles].[title]+'x'+@p_pubName), [titles].[type]=@t_type " +
        "FROM [publishers] p " +
        "WHERE (([titles].[title_id]=@param1) AND ([titles].[pub_id]=p.[pub_id]))";
        [TestMethod]
        public void TestUpdateStatementWithParamSource()
        {
            TitleMeta TITLE = new TitleMeta("t");
            PublisherMeta PUB = new PublisherMeta("p");
            StatementScope scope = this.NewStatement;

            Publisher pub = new Publisher();
            pub.PubName = "My publishing house";
            scope.ParamMap.EntitySourceMap.SetEntity(PUB, pub);

            Title title = new Title();
            title.Type = "business";
            scope.ParamMap.EntitySourceMap.SetEntity(TITLE, title);

            UpdateStatement statement = (UpdateStatement)scope.Update(TITLE)
                .Set(TITLE.title, TITLE.title + "x" + PUB.pubName.ParamSource())
                .Set(TITLE.type, TITLE.type.ParamSource().EnableIf(true))
                    .From(PUB)
                    .Where(TITLE.titleId == "PC8888" & TITLE.pubId == PUB.pubId)
                    .DebugDump("Update Statement With ParamSource()");

            Assert.AreEqual(updateStatementWithParamSource, RemoveUnnecessarySymbols(statement.ToString()));

        }
    }
}
