using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NorthwindDataModel.ServiceInterfaces
{
    public interface IOrderService
    {
        Order GetOrder(int orderID);
        void ConfirmOrder(Customer customer, Order order);
    }
}
