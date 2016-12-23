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
    /// objects in an execution code path to add validation results.
    /// </summary>
    public class ValidationContext : CustomCollaboration
    {
        private List<ValidationEntry> validationEntries = new List<ValidationEntry>();

        private bool hasError;
        private bool hasWarning;

        public static ValidationContext Current
        {
            get { return Get<ValidationContext>(); }
        }

        public static void AddError(object target, string message)
        {
            if (Current != null)
            {
                Current.validationEntries.Add(new ValidationError() { TargetObject = target, Message = message });
                Current.hasError = true;
            }
        }

        public static void AddWarning(object target, string message)
        {
            if (Current != null)
            {
                Current.validationEntries.Add(new ValidationWarning() { TargetObject = target, Message = message });
                Current.hasWarning = true;
            }
        }

        public bool IsValid
        {
            get
            {
                return !this.hasWarning && !this.hasError;
            }
        }

        public bool IsAcceptable
        {
            get
            {
                return !this.hasError;
            }
        }

        /// <summary>
        /// Throw a single exception for all the validations.
        /// </summary>
        public void ThrowOnError()
        {
            if (!IsAcceptable)
            {
                throw new ValidationException(this);
            }
        }

        /// <summary>
        /// Dump all validation messages accumulated so far.
        /// </summary>
        [Conditional("DEBUG")]
        public void Dump()
        {
            if (this.hasError || this.hasWarning)
            {
                Debug.WriteLine("ValidationContext has errors/warnings.  Dumping validation results:");
                foreach (var entry in validationEntries)
                    Debug.WriteLine(string.Format("  {0}", entry.ToString()));
            }
            else
            {
                Debug.WriteLine("ValidationContext has no errors/warnings.");
            }
        }

    }

    public abstract class ValidationEntry
    { 
        public object TargetObject { get; set; }
        public string Message { get; set; }
    }

    public class ValidationError : ValidationEntry
    {
        public override string ToString()
        {
            return string.Format("!Error!:  Target={0}, Message={1}", this.TargetObject, this.Message);
        }   
    }
    
    public class ValidationWarning : ValidationEntry
    {
        public override string ToString()
        {
            return string.Format("Warning:  Target={0}, Message={1}", this.TargetObject, this.Message);
        }
    }

    public class ValidationException : Exception
    {
        private ValidationContext validationContext;

        public ValidationException(ValidationContext validationContext)
            : base("Validation error")
        {
            this.validationContext = validationContext;
        }
    }
}
