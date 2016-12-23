using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SOCF;
using NorthwindDataModel.ServiceInterfaces;
using NorthwindDataModel;
using NorthwindDataModel.Collaboration.Entities;

namespace NorthwindServices.Services
{
    public class OrderService : IOrderService
    {

        #region IOrderService Members

        public Order GetOrder(int orderID)
        {
            // The collaboration context usage is actually
            // similar to a linq data context.  The difference is
            // that the collaboration context is available both in this method
            // and in all the nested calls until the using() block is exited.
            // It is possible to create a shared Linq data context if you 
            // want to use the same data context in the nested calls.
            /*using (var c = new CollaborationContext<NorthwindDataContext>(new NorthwindDataContext()))
            {
            }*/

            using (var db = new NorthwindDataContext())
            {
                return db.Orders.FirstOrDefault(o => o.OrderID == orderID);
            }

        }

        public void ConfirmOrder(Customer customer, Order order)
        {
            // We are starting a custom collaboration context
            // for the order processing pipeline to use common objects.
            // This means, we won't bother passing around objects in
            // paramters.  This makes the code more context sensitive
            // and less contractual.  This kind of design provides
            // a lot of flexibility, but must not be taken to an extreme.
            // Examples given here are well known patterns for which this
            // style of programming is very useful.
            using (var orderConfirmation = new OrderConfirmation())
            {
                // Notice that the operation log is optional at this point.
                // If the caller starts an operation log collaboration context,
                // the following call will actually add a message, otherwise it is just a noop.
                LoggingContext.Add("Processing order");
                // Set up all the objects and services that contribute to the collaboration.
                // You could also do this in the object initializer block:  new OrderConfirmation() { Customer =... }
                orderConfirmation.Order = order;
                orderConfirmation.Customer = customer;
                orderConfirmation.EmailService = new EmailService();        // We could also provide the email service through another collaboration context object.  Here, we opted to make it part of the OrderConfirmation collaboration.

                // All operations after this point rely on the current OrderCollaboration context.
                // We just call simple methods for processing steps.
                // However, these steps could have been implemented by seperate objects each
                // performing a separate task of the confirmation process.
                // The OrderConfirmation context would always be available during the calls.
                InitialValidate();
                Calculate();        // Calculate data based on order details.
                CompleteOrder();    // Complete rest of the order data.

                Validate();

                InsertOrder();      // Commit
                SendConfirmation(); // Send a confirmation email

                // Here's an alternative approach:
                //   foreach (var processors in Processors)
                //      processors.Process();
                // This approach is more extensible.  You can add new processors without touching the exiting code.
                // The benefit of using such a collaboration context is more obvious in such a scenario.

                LoggingContext.Add("Order Confirmation successfuly completed");
                LoggingContext.Add(string.Format("Order id = {0}", order.OrderID));
            }
        }

        private void InitialValidate()
        {
            // Notice that the validation code will execute only if
            // there is a validation context provided by the caller.
            if (ValidationContext.Current != null)
            {
                // We accumulate all the validation results in validation context, if availabe.

                LoggingContext.Add("Inital validation");
                Order order = OrderConfirmation.Current.Order;
                if (order.Order_Details.Count == 0)
                    ValidationContext.AddError(order, "No order details!");

                if (order.RequiredDate < DateTime.Today)
                    ValidationContext.AddError(order, "Required date must be in the future");

                // The validation logic can be also made much more extensible the following approach:
                //   foreach (var validator in Validators)
                //      validator.Validate();
                // In this scenario, each validation object wouldn't require all the necessary 
                // data it will use, rather it would just use whatever is provided in the context.

                LoggingContext.Add("Initial validation completed");

                ValidationContext.AddWarning(order, "This is just sample warning message");

                ValidationContext.Current.ThrowOnError();
            }
        }

        private void Validate()
        {
            // Notice that the validation code will execute only if
            // there is a validation context provided by the caller.
            if (ValidationContext.Current != null)
            {
                LoggingContext.Add("Validating order");
                Order order = OrderConfirmation.Current.Order;

                // Do further validation here..
                
                LoggingContext.Add("Order validated");

                ValidationContext.Current.ThrowOnError();
            }
        }

        private void Calculate()
        {
            LoggingContext.Add("Calculating");
            // Get order in context
            Order order = OrderConfirmation.Current.Order;
            
            // Do some calculations with the order and order details in the context.
            decimal sum = 0;
            
            foreach (var item in order.Order_Details)
            {
                Product product = ProductRepository.GetProduct(item.ProductID); // Notice that the product repository will use the identity map if it is provided by the caller.  Otherwise it will go to db.
                item.UnitPrice = product.UnitPrice ?? 0;
                sum += item.UnitPrice * item.Quantity;
            }

            order.TotalCharge = sum;

            LoggingContext.Add("Calculated");
        }

        private void CompleteOrder()
        {
            LoggingContext.Add("Completing order");
            Order order = OrderConfirmation.Current.Order;
            //order.EmployeeID = EmployeeTransaction.Current.Employee.EmployeeID;
            LoggingContext.Add("Order completed");
        }

        private void InsertOrder()
        {
            LoggingContext.Add("Inserting order data into db");
            using (var db = new NorthwindDataContext())
            {
                // Get order in context
                Order order = OrderConfirmation.Current.Order;
                order.CustomerID = OrderConfirmation.Current.Customer.CustomerID;
                order.OrderDate = DateTime.Today;
                
                db.Orders.InsertOnSubmit(order);

                db.SubmitChanges();

                LoggingContext.Add("Inserted into db");
            }
        }

        private void SendConfirmation()
        {
            OrderConfirmation.Current.EmailService.Send(
                OrderConfirmation.Current.Customer.Email,
                "Order Confirmation",
                string.Format("Your order {0} has been received.", OrderConfirmation.Current.Order.OrderID));
        }

        #endregion

    }
}
