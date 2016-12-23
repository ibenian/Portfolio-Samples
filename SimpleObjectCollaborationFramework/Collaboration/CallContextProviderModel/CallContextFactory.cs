using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SOCF.CallContextProviderModel
{
    /// <summary>
    /// This simple provider model allows you to implement you to implement a different 
    /// call context mechanism and replace the existing one.
    /// </summary>
    public static class CallContextFactory
    {
        public static ICallContextProvider Instance { get; set; }

        static CallContextFactory()
        {
            // Default provider
            Instance = new DefaultCallContextProvider();
        }
    }
}
