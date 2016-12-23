using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SOCF.CallContextProviderModel
{
    /// <summary>
    /// Implement this interface to provide access to some kind of call context.
    /// Currently there are four such mechanisms provided by .NET.
    ///   1. ThreadStatic  (Least reliable, most efficient)
    ///   2. CallContext   (Reliable for multi-thread non ASP.NET apps including remoting)
    ///   3. HTTPContext   (Most reliable for ASP.NET applications and web services.)
    ///   4. OperationContext (Reliable for WCF services.)
    ///   
    /// SOCF currently has a default implementation that uses CallContext, and
    /// an HTTPContext implemenation that you can find in this solution.
    /// </summary>
    public interface ICallContextProvider
    {
        void SetData(string key, object data);
        object GetData(string key);
    }
}
