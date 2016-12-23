using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NorthwindServices.Services;
using NorthwindDataModel;
using NorthwindDataModel.Collaboration.Entities;
using SOCF;

namespace TestCollaborations
{
    /// <summary>
    /// This test demonstrates the concept of collaboration context objects
    /// in a simple order processing pipeline based on Northwind data model.
    /// The data model contains just the necessary minimum to show the main idea.
    /// 
    /// Notice that callers can opt to add a context at any point as well as the callee
    /// can opt to use or not use those collaboration objects.
    /// Thus, independent parts of a complex system can be developed independently.
    /// The collaboration framework takes care of providing the necessary context
    /// to all the objects that contribute to a task.
    /// 
    /// You may need to download and install the Northwind database from MS SQL samples.
    /// Also make sure the connection string in web.config is properly set in &lt;connectionStrings&gt; tag.
    /// </summary>
    [TestClass]
    public class TestOrderProcessing
    {
        public TestOrderProcessing()
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

        /// <summary>
        /// Create a dummy order for a customer, add some order items and
        /// process the order using OrderService.  The OrderService can
        /// use any collaboration context objects that is already started up to here.
        /// The following tests show how a ValidationContext and a LoggingContext
        /// can be started before the order service is invoked.
        /// </summary>
        [TestMethod]
        public void TestOrderConfirmation()
        {
            // Select a customer.
            CustomerService customerService = new CustomerService();
            Customer customer = customerService.GetCustomer("ALFKI");

            // Create an order for him.
            Order order = new Order();
            order.Order_Details.Add(new Order_Detail() { ProductID = 1, Quantity = 10 });
            order.Order_Details.Add(new Order_Detail() { ProductID = 2, Quantity = 2 });
            order.Order_Details.Add(new Order_Detail() { ProductID = 3, Quantity = 5 });

            // Display the order.

            // Confirm and commit the order.
            OrderService orderService = new OrderService();
            orderService.ConfirmOrder(customer, order);
        }


        /// <summary>
        /// Just by adding a new collaboration context, we can change the
        /// behavior of the whole processing pipeline no matter how deep it goes.
        /// With the addition of validation context, the same order processing
        /// will also do validation.
        /// </summary>
        [TestMethod]
        public void TestOrderConfirmationWithAdditionalValidationContext()
        {
            using (var validation = new ValidationContext())
            {
                try
                {
                    TestOrderConfirmation();
                }
                finally
                {
                    // Dump the validation context in any case
                    validation.Dump();
                }
            }
        }

        /// <summary>
        /// Just by adding a new collaboration context, we can change the
        /// behavior of the whole processing pipeline no matter how deep it goes.
        /// With the addition of logging context, the same order processing
        /// will now also log information about the steps that it performs.
        /// </summary>
        [TestMethod]
        public void TestOrderConfirmationWithAdditionalLoggingAndValidationContexts()
        {
            using (var log = new LoggingContext())
            {
                try
                {
                    TestOrderConfirmationWithAdditionalValidationContext();       // The call context will now have both logging and validation context, as well as the OrderConfirmation collaboration inside the order service.
                }
                finally
                {
                    // All the logging is done so far, we can now check what has been written to log.
                    log.Dump();
                }
            }
        }

        /// <summary>
        /// Just by adding a new collaboration context, we can change the
        /// behavior of the whole processing pipeline no matter how deep it goes.
        /// We add an identity map scope for Product type, so ProductRepository
        /// can reuse objects if they already exist in this map.
        /// This style of programming reduces db roundtrips, but also provides better
        /// lifetime control over the cached objects.  This is because we can chose
        /// where the caching starts and ends, rather than having to cache everything
        /// globally or per user.
        /// </summary>
        [TestMethod]
        public void TestOrderConfirmationWithIdentityMap()
        {
            // Create an identity map for products.
            // Any nested call within this using block can now cache and reuse objects
            // by their key.
            // The ProductRepository will log information about whether it could find
            // a product in the scope, or it loaded from db.  Check the debug output after log.Dump().
            // See the identity map test for a more detailed explanation of the identity map.
            using (var map = new IdentityMap<Product>(IdentityMapScope.AllParents))
            {
                // Also create a logging context.
                using (var log = new LoggingContext())
                {
                    try
                    {
                        TestOrderConfirmationWithAdditionalValidationContext();       // The call context will now have both logging and validation context, as well as the OrderConfirmation collaboration inside the order service.
                    }
                    finally
                    {
                        // All the logging is done so far, we can now check what has been written to log.
                        log.Dump();
                    }
                }
            }
        }

    }
}
