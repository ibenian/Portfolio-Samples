using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SOCF;
using NorthwindDataModel;

namespace TestCollaborations
{
    /// <summary>
    /// This test demonstrates the behavior of IdentityMap as well as 
    /// how it is used as a collaboration context.
    /// </summary>
    [TestClass]
    public class TestIdentityMap
    {
        public TestIdentityMap()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        #region Tests
        /// <summary>
        /// Create a simple identity map collaboration context, and cache
        /// some products in it.  Then try accessing those inside a nested method call.
        /// </summary>
        [TestMethod]
        public void TestIdentityMapSimple()
        {
            // Create a simple scope for caching product objects.
            // Everyting within the using() block will be able to access this map.
            using (var map = new IdentityMap<Product>(IdentityMapScope.Local))
            {
                CacheProducts(1, 2, 3);
                map.DumpObjects();  // Dump objects to debug output.
                ProductsMustExist(1, 2, 3);
                ProductsMustNotExist(4, 5, 6);
                UncacheProducts(1, 2, 3);
                map.DumpObjects();  // Dump objects to debug output.
                ProductsMustNotExist(1, 2, 3);
            }
        }

        /// <summary>
        /// Create 4 levels of nested identity maps.
        /// For demonstration purposes nestings are in the same method,
        /// but they could just as well be placed in other methods, or even
        /// methods of other classes.
        /// An identity map is specific to a type.  Separate types will have entirely
        /// separete nesting hierarchies without any entanglement with maps of other types.
        /// </summary>
        [TestMethod]
        public void TestNestedIdentityMap()
        {
            // Topmost scope
            using (var map1 = new IdentityMap<Product>(IdentityMapScope.Local))
            {
                CacheProducts(1, 2, 3);
                map1.DumpObjects();  // Dump objects to debug output.
                ProductsMustExist(1, 2, 3);

                // Second level nested scope
                using (var map2 = new IdentityMap<Product>(IdentityMapScope.AllParents))
                {
                    CacheProducts(4, 5, 6);
                    map2.DumpObjects();
                    ProductsMustExist(4, 5, 6);     // Products of local scope
                    ProductsMustExist(1, 2, 3);     // Products of the parent scope must also be accessible here

                    // Third level nested scope
                    using (var map3 = new IdentityMap<Product>(IdentityMapScope.AllParents))
                    {
                        CacheProducts(7, 8, 9);
                        map3.DumpObjects();
                        ProductsMustExist(7, 8, 9);     // Products of local scope
                        ProductsMustExist(4, 5, 6);     // Products of the parent scope must also be accessible here
                        ProductsMustExist(1, 2, 3);     // Products of the top scope must also be accessible here

                        // Fourth level nested scope
                        using (var map4 = new IdentityMap<Product>(IdentityMapScope.Parent))    // Notice that this can only access the immediate parent scope and no more than that.
                        {
                            CacheProducts(10, 11, 12);
                            map4.DumpObjects();
                            ProductsMustExist(10, 11, 12);  // Products of local scope
                            ProductsMustExist(7, 8, 9);     // Products of the immediate parent scope are also accessible here
                            ProductsMustNotExist(4, 5, 6);  // Products of the further parent scopes are not accessible here
                            ProductsMustNotExist(1, 2, 3);     // Products of the further parent scopes are not accessible here
                        }
                    }
                }

                ProductsMustNotExist(4, 5, 6);      // Products of the nested scope must not exist here
                ProductsMustNotExist(7, 8, 9);      // Products of the nested scope must not exist here
                ProductsMustNotExist(10, 11, 12);   // Products of the nested scope must not exist here
            }
        }

        #endregion

        #region Helpers

        private void ProductsMustExist(params int[] productIDs)
        {
            foreach (int productID in productIDs)
            {
                Product product = IdentityMap<Product>.Get(productID);
                Assert.IsNotNull(product);
                Assert.AreEqual(product.ProductID, productID);
            }
        }

        private void ProductsMustNotExist(params int[] productIDs)
        {
            foreach (int productID in productIDs)
            {
                Product product = IdentityMap<Product>.Get(productID);
                Assert.IsNull(product);
            }
        }

        private void CacheProducts(params int[] productIDs)
        {
            foreach (int productID in productIDs)
                IdentityMap<Product>.Set(productID, new Product() { ProductID = productID, ProductName = "Product " + productID.ToString(), UnitPrice = productID * 10 });
        }

        private void UncacheProducts(params int[] productIDs)
        {
            foreach (int productID in productIDs)
                IdentityMap<Product>.Set(productID, null);
        }

        #endregion
    }
}
