using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SOCF
{
    /// <summary>
    /// Strongly typed collaboration class
    /// represents a collaboration of type T.
    /// 
    /// The collaboration context will just be accessible during the
    /// along the code execution path until using() block is exited.
    /// All objects that are in the calling method as well as all nested methods
    /// will be able to access this shared collaboration context.  
    ///         
    /// Objects can thus interact as if they were in a local variable scope.  The lifetime
    /// of provided objects are all determined by the starter of collaboration.
    /// This allows organized code to always remain organized independently of the
    /// current task that they are performing.
    /// A better approach is to create a separate custom collaboration entity
    /// that derives from CustomCollaboratoin.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// 
    ///     // To start a new collaboration use:
    ///     using (new CollaborationContext(new EmailService()))
    ///     {
    ///         ..
    ///     }
    ///      
    ///     // or 
    ///     using (new Collaboration<EmailService>(new EmailService()))
    ///     {
    ///          ..
    ///     }
    ///    
    ///     // Here's an example code snippet to access this context object and the provided object instance in a nested method call:
    ///     EmailService emailService = CollaborationContext&lt;EmailService&gt;.Get().Value;
    ///     if (emailService != null)
    ///         emailService.SendEmail(...);
    /// ]]>
    /// </example>
    /// <typeparam name="T">Type for which a collaboration is to be started.</typeparam>
    public class CollaborationContext<T> : NamedCollaborationContext
    {
        public CollaborationContext(params object[] contextObjects)
            : base(typeof(T).FullName, contextObjects)
        { }

        public CollaborationContext(T contextObject)
            : base(typeof(T).FullName, contextObject)
        { }

        public T Value
        {
            get { return (T)this.ContextObjects[0]; } 
        }

        public static CollaborationContext<T> Current
        {
            get
            {
                return (CollaborationContext<T>)NamedCollaborationContext.Get(typeof(T).FullName);
            }
        }

    }
}
