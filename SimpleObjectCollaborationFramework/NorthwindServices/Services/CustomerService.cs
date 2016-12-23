using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NorthwindDataModel;

namespace NorthwindServices.Services
{
    public class CustomerService
    {
        public Customer GetCustomer(string customerID)
        {
            using (var db = new NorthwindDataContext())
            {
                return db.Customers.FirstOrDefault(c => c.CustomerID == customerID);
            }

        }

    }
}
