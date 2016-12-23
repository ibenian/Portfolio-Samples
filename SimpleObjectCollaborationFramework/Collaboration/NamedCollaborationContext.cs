using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;

namespace SOCF
{
    public class NamedCollaborationContext : IDisposable
    {
        private NamedCollaborationContext superContext;      // previously existing context that this context had to replace
        private object[] contextObjects;
        private string name;
        private bool active;      // set to true once this collaboration context object is put to context.

        public Action<NamedCollaborationContext> ContextReactivated;
        public Action<NamedCollaborationContext> ContextRevoked;
        public Action<NamedCollaborationContext> ContextDeactivated;

        public NamedCollaborationContext(string name, params object[] contextObjects)
        {
            if (name == null)
                this.name = this.GetType().FullName;
            else
                this.name = name;
            this.contextObjects = contextObjects;
            this.superContext = OverrideContext(this);
        }
        
        public string CollaborationName
        {
            get { return name; }
        }

        public NamedCollaborationContext SuperContext
        {
            get { return this.superContext; }
        }

        /// <summary>
        /// Called when this context is activated again when a nested context of same type exits its scope.
        /// </summary>
        protected virtual void OnContextReactivated()
        {
            if (ContextReactivated != null)
                ContextReactivated(this);
        }

        /// <summary>
        /// Called when the current context is overridden by a nested context.
        /// </summary>
        protected virtual void OnContextDeactivated()
        {
            if (ContextDeactivated != null)
                ContextDeactivated(this);
        }

        /// <summary>
        /// Called when the current context is disposed and revoked from call context.
        /// </summary>
        protected virtual void OnRevoke()
        {
            if (ContextRevoked != null)
                ContextRevoked(this);
        }

        /// <summary>
        /// Returns true, if this collaboration context object has been started and put to call context.
        /// </summary>
        public bool Active
        {
            get { return active; }
        }
    
        #region IDisposable Members

        public void Dispose()
        {
            RevokeContext(this);
        }

        #endregion

        private static NamedCollaborationContext OverrideContext(NamedCollaborationContext newContext)
        {
            object value = CallContextProviderModel.CallContextFactory.Instance.GetData(newContext.name);
            if (value != null && !(value is NamedCollaborationContext))
                throw new ArgumentException(string.Format("Name '{0}' specied for the CollaborationContext is already in use for another purpose!", newContext.name));
            
            NamedCollaborationContext prevContext = value as NamedCollaborationContext;
            CallContextProviderModel.CallContextFactory.Instance.SetData(newContext.name, newContext);
            newContext.active = true;
            return prevContext;
        }

        private static NamedCollaborationContext RevokeContext(NamedCollaborationContext context)
        {
            object value = CallContextProviderModel.CallContextFactory.Instance.GetData(context.name);
            if (value != null && !(value is NamedCollaborationContext))
                throw new ArgumentException(string.Format("Name '{0}' specied for the CollaborationContext is already in use for another purpose!", context.name));

            NamedCollaborationContext prevContext = value as NamedCollaborationContext;

            if (prevContext != context)
                throw new ArgumentException(string.Format("CollaborationContext '{0}' is not the immediate context!", context.name));

            context.OnRevoke();

            context.active = false;  // deactivate the context
            CallContextProviderModel.CallContextFactory.Instance.SetData(context.name, context.superContext);    // revert back to the old context.
            context.OnContextDeactivated();
            if (context.superContext != null)
            {
                context.superContext.OnContextReactivated();        // notify supercontext
            }
            return context.superContext;
        }

        public static NamedCollaborationContext Get(string name)
        {
            return CallContextProviderModel.CallContextFactory.Instance.GetData(name) as NamedCollaborationContext;
        }

        public object[] ContextObjects
        {
            get { return contextObjects; }
        }
    }
}
