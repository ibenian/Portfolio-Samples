using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SOCF.CallContextProviderModel;
using System.Web;

namespace CallContextProviderForASPNET
{
    /// <summary>
    /// This call context provider is safer to use with ASP.NET applications ans web services.
    /// ASP.NET makes sure that the context is always kept consistent with the
    /// current request by migrating it when the used thread changes.
    /// </summary>
    public class CallContextProviderForASPNET : ICallContextProvider
    {

        #region ICallContextProvider Members

        public void SetData(string key, object data)
        {
            HttpContext.Current.Items[key] = data;
        }

        public object GetData(string key)
        {
            return HttpContext.Current.Items[key];
        }

        #endregion
    }
}
