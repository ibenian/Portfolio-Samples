using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using NorthwindServices.Services;
using NorthwindDataModel;
using SOCF;
using NorthwindDataModel.Collaboration.Entities;

public partial class _Default : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Test call context.
        // You may need to download and install the Northwind database from MS SQL samples.
        // Also make sure the connection string in web.config is properly set in <connectionStrings> tag.

        TestOrderConfirmation();
        TestOrderConfirmationWithIdentityMap();
    }

    /// <summary>
    /// Create a dummy order for a customer, add some order items and
    /// process the order using OrderService.  The OrderService can
    /// use any collaboration context objects that is already started up to here.
    /// The following tests show how a ValidationContext and a LoggingContext
    /// can be started before the order service is invoked.
    /// </summary>
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
    /// We add an identity map scope for Product type, so ProductRepository
    /// can reuse objects if they already exist in this map.
    /// This style of programming reduces db roundtrips, but also provides better
    /// lifetime control over the cached objects.  This is because we can chose
    /// where the caching starts and ends, rather than having to cache everything
    /// globally or per user.
    /// </summary>
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
