using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SOCF
{
    /// <summary>
    /// Base class for a custom collaboration entity.
    /// A custom collaboration entity provides instances of objects
    /// that can be used in a collaboration.
    /// You can derive from this class, implement properties that
    /// return object instances.
    /// 
    /// The collaboration context will just be accessible during the
    /// along the code execution path until using() block is exited.
    /// All objects that are in the calling method as well as all nested methods
    /// will be able to access this shared collaboration context.
    /// Objects can thus interact as if they were in the same method scope.
    /// This allows organized code to always remain organized independently of the
    /// current task that they are performing.
    /// A better approach is to create a separate custom collaboration entity
    /// that derives from CustomCollaboratoin.
    /// 
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// 
    ///     // To start a new collaboration use:
    ///     using (new OrderConfirmation())
    ///     {
    ///         ..
    ///     }
    ///     
    ///     // Here's an example code snippet to access this collaboration entity and the provided object instances in a nested method call:
    ///     OrderConfirmation confirmation = OrderConfirmation.Current;
    ///     if (confirmation != null)
    ///     {
    ///         confirmation.Subject = string.Format("Order {0} has been received.", confirmation.Order.ID);
    ///         confirmation.Email.Body = ...;
    ///         confirmation.EmailService.Send();
    ///     }
    /// ]]>
    /// </example>
    public class CustomCollaboration : NamedCollaborationContext
    {
        public CustomCollaboration()
            : base(null)    // Uses full name of this class.
        {
               
        }

        public static T Get<T>() where T : CustomCollaboration
        {
            return (T)NamedCollaborationContext.Get(typeof(T).FullName);
        }
    }
}
