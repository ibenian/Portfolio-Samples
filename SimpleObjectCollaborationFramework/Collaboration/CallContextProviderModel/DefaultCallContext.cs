using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;

namespace SOCF.CallContextProviderModel
{
    public class DefaultCallContextProvider : ICallContextProvider
    {
        #region ICallContextProvider Members

        public void SetData(string key, object data)
        {
            CallContext.SetData(key, data);
        }

        public object GetData(string key)
        {
            return CallContext.GetData(key);
        }

        #endregion
    }
}
