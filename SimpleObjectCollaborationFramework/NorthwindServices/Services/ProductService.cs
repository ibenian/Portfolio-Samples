using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NorthwindDataModel;

namespace NorthwindServices.Services
{
    public class ProductService
    {
        public Product GetProduct(int productID)
        {
            using (var db = new NorthwindDataContext())
            {
                return db.Products.FirstOrDefault(p => p.ProductID == productID);
            }

        }

    }
}
