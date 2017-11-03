using System;
using System.Runtime.CompilerServices;

namespace toofz.Tests
{
    internal static class ExceptionHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowException()
        {
            throw new Exception("Thrown test exception");
        }

        public static Exception GetThrownException()
        {
            try
            {
                ThrowException();
            }
            catch (Exception ex)
            {
                return ex;
            }

            // Unreachable
            return null;
        }

        private static void ThrowExceptionWithInnerException()
        {
            try
            {
                ThrowException();
            }
            catch (Exception ex)
            {
                throw new Exception("Thrown test exception with inner exception", ex);
            }
        }

        public static Exception GetThrownExceptionWithInnerException()
        {
            try
            {
                ThrowExceptionWithInnerException();
            }
            catch (Exception ex)
            {
                return ex;
            }

            // Unreachable
            return null;
        }
    }
}
