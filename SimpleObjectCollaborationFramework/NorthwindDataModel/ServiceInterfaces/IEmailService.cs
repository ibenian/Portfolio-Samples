using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NorthwindDataModel.ServiceInterfaces
{
    public interface IEmailService
    {
        void Send(string toAddress, string subject, string body);
    }
}
