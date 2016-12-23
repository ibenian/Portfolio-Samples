using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SOCF;
using System.Diagnostics;

namespace NorthwindDataModel.Collaboration.Entities
{
    /// <summary>
    /// This is a collaboration context object that allows
    /// objects in an execution code path to write trace
    /// information about the current operation.  Since the
    /// log information is accumulated on this instance,
    /// each thread will have its own local log.  This
    /// ensures that the related messages are kept contiguous
    /// and in correct temporal order.
    /// </summary>
    public class LoggingContext : CustomCollaboration
    {
        private List<string> messages = new List<string>();
        /// <summary>
        /// Used like a singleton, but it actually is a shared instance
        /// in this collaboration context.  Start an LoggingContext
        /// context as follows:
        ///     using (new LoggingContext())
        ///     {
        ///         ...
        ///     }
        ///     
        /// To contribute to logging, use:
        ///     LoggingContext.Add("Step 1 has been completed");
        ///     
        /// Notice that there is no Clear() method.  Instead, the starter of
        /// this collaboration should assume that the logging will happen
        /// within using() block and a new logging context should be started for
        /// a different collaboration.
        /// </summary>
        public static LoggingContext Current
        {
            get { return Get<LoggingContext>(); }
        }


        public static void Add(string message)
        {
            if (Current != null)
                Current.messages.Add(message);
        }

        protected override void OnRevoke()
        {
            base.OnRevoke();

            // Dump all the messages accumulated so far to debug output.
            Dump();
        }

        /// <summary>
        /// Dump all messages accumulated so far.
        /// </summary>
        [Conditional("DEBUG")]
        public void Dump()
        {
            foreach (string s in messages)
                Debug.WriteLine(s);
        }
    }
}
