using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SOCF;
using NorthwindDataModel.ServiceInterfaces;

namespace NorthwindDataModel.Collaboration.Entities
{
    public class OrderConfirmation : CustomCollaboration
    {
        public Customer Customer { get; set; }
        public Order Order { get; set; }
        public IEmailService EmailService { get; set; }


        public static OrderConfirmation Current
        {
            get { return Get<OrderConfirmation>(); }
        }
    }
}
