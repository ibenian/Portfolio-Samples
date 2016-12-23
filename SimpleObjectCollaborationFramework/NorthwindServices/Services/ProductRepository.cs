using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NorthwindDataModel;
using SOCF;
using NorthwindDataModel.Collaboration.Entities;

namespace NorthwindServices.Services
{
    /// <summary>
    /// This example demonstrates how a repository class
    /// can use the IdentityMap to cache objects and reuse them before
    /// doing a db roundtrip.
    /// </summary>
    public class ProductRepository
    {
        /// <summary>
        /// Get a product from the product repository.
        /// First check the current identity map scope and then query db if it doesn't exist.
        /// </summary>
        public static Product GetProduct(int productID)
        {
            // Check if the given product exists in the current scope.
            Product product = IdentityMap<Product>.Get(productID);
            if (product != null)
            { 
                // Product found in identity map
                LoggingContext.Add(string.Format("Product {0} found in identity map.  Reusing.", productID));
            }
            else
            {
                // Doesn't exist, load from db.
                ProductService productService = new ProductService();
                product = productService.GetProduct(productID);
                // Cache it into the current scope if it exists.
                IdentityMap<Product>.Set(productID, product);
                LoggingContext.Add(string.Format("Product {0} not found in identity map.  Loaded from db and cached.", productID));
            }

            return product;
        }
    }
}
