using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SOCF
{
    public enum IdentityMapScope
    {
        Local = 0,      // Access local objects only
        Parent = 1,     // Access local objects first, then objects in parent scope
        AllParents = 2  // Access local objects first, then objects in parent scopes searching up the nesting chain
    }

    /// <summary>
    /// This is a simple identity map implementation that can be used
    /// to cache objects by a key and type.  It is based on the same
    /// collaboration context idea.  When an IdentityMap is started,
    /// it is available until the using() block exits, both in the 
    /// current method and in the nested method calls.  So the service
    /// classes in the sample code can access this before going to database.
    /// This class also shows how super contexts (context objects that this object overrides),
    /// can be used to extend collaboration scope.
    /// </summary>
    public class IdentityMap<T> : CustomCollaboration
    {
        private IdentityMapScope scope;
        private Dictionary<object, T> map = new Dictionary<object, T>();

        public IdentityMap(IdentityMapScope scope)
        {
            this.scope = scope;
        }

        private T GetObjectLocal(object key)
        {
            T obj;
            if (map.TryGetValue(key, out obj))
                return obj;
            else
                return default(T);
        }

        public T GetObject(object key)
        {
            T obj = GetObjectLocal(key);
            if (obj == null)
            {
                // If scope allows looking in parents
                if (scope == IdentityMapScope.Parent)
                {
                    IdentityMap<T> parentScope = this.ParentScope;
                    if (parentScope != null)
                        obj = parentScope.GetObjectLocal(key);  // Search only the immediate parent.
                }
                else if (scope == IdentityMapScope.AllParents)
                {
                    IdentityMap<T> parentScope = this.ParentScope;
                    if (parentScope != null)
                        obj = parentScope.GetObject(key);   // Search parent and its parents.
                }
            }

            return obj;
        }

        public void SetObject(object key, T obj)
        {
            if (obj == null)
                map.Remove(key);
            else
                map[key] = obj;
        }

        IdentityMap<T> ParentScope
        {
            get
            {
                return (IdentityMap<T>)this.SuperContext;
            }
        }

        /// Notice that there is no Clear() method.  Instead, the starter of
        /// this collaboration should assume that the logging will happen
        /// within using() block and a new logging context should be started for
        /// a different collaboration.
        /// </summary>
        public static IdentityMap<T> Current
        {
            get { return Get<IdentityMap<T>>(); }
        }

        /// <summary>
        /// Set an object on the current identity map.
        /// </summary>
        public static void Set(object key, T obj)
        {
            if (Current != null)
            {
                // There is a context
                Current.SetObject(key, obj);
            }
        }

        /// <summary>
        /// Get an object from the current scope,
        /// or if doesn't exist, look for it in the parents
        /// if allowed by the scope.
        /// </summary>
        public static T Get(object key)
        {
            if (Current != null)
            {
                // There is a context
                return Current.GetObject(key);
            }
            else
                return default(T);
        }

        [Conditional("DEBUG")]
        public static void Dump()
        {
            if (Current != null)
            {
                Current.DumpObjects();
            }
        }

        [Conditional("DEBUG")]
        public void DumpObjects()
        {
            Debug.WriteLine(string.Format("Identity map dump for {0}.  {1} objects cached.  Scope={2}.", typeof(T).FullName, map.Count, scope));

            foreach (var kv in this.map)
            {
                Debug.WriteLine(string.Format("  Key={0}\t\tObject={1}", kv.Key, kv.Value));
            }
        }
    }
}
