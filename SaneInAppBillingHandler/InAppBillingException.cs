using System;

namespace SaneInAppBillingHandler
{
    public class InAppBillingException : Exception
    {
        public InAppBillingException(string message)
            : base(message)
        { }
    }
}