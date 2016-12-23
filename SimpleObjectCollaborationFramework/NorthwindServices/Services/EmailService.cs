using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NorthwindDataModel.ServiceInterfaces;
using System.Net.Mail;
using System.Diagnostics;
using NorthwindDataModel.Collaboration.Entities;

namespace NorthwindServices.Services
{
    public class EmailService : IEmailService
    {

        #region IEmailService Members

        public void Send(string toAddress, string subject, string body)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.To.Add(toAddress);
            mailMessage.Subject = subject;
            // Prepare the message and send.
            mailMessage.From = new MailAddress("NorthwindWithObjectCollaboration@Northwind.com");
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = body;
            SmtpClient client = new SmtpClient();
            // client.Send(mailMessage);
            // We don't actually send the message for testing purposes.

            // If there is any logging context, we just contribute to it by adding a message.
            LoggingContext.Add("Email Service has sent a message to " + toAddress);
        }

        #endregion
    }
}
