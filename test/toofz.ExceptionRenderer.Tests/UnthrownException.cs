using System;

namespace toofz.Tests
{
    internal sealed class UnthrownException : Exception
    {
        public UnthrownException(string stackTrace)
        {
            this.stackTrace = stackTrace;
        }

        public override string StackTrace => stackTrace;
        private readonly string stackTrace;
    }
}
