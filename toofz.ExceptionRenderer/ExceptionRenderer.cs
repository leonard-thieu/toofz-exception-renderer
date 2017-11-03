using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using log4net;
using log4net.ObjectRenderer;

namespace toofz
{
    /// <summary>
    /// Custom log4net renderer for <see cref="Exception"/>.
    /// </summary>
    public sealed class ExceptionRenderer : IObjectRenderer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExceptionRenderer));

        /// <summary>
        /// Renders an object of type <see cref="Exception"/> similar to how Visual Studio Exception Assistant 
        /// renders it.
        /// </summary>
        /// <param name="rendererMap">Not used.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="writer">The writer.</param>
        public void RenderObject(RendererMap rendererMap, object exception, TextWriter writer)
        {
            RenderObject(rendererMap, exception, writer, false);
        }

        /// <summary>
        /// Renders an object of type <see cref="Exception"/> similar to how Visual Studio Exception Assistant 
        /// renders it.
        /// </summary>
        /// <param name="rendererMap">Not used.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="suppressFileInfo">
        /// Suppresses file information in stack traces. This is used for repeatable tests.
        /// </param>
        internal void RenderObject(RendererMap rendererMap, object exception, TextWriter writer, bool suppressFileInfo)
        {
            var ex = (Exception)exception;
            var type = ex.GetType();

            var indentedWriter = writer as IndentedTextWriter;
            if (indentedWriter == null)
            {
                indentedWriter = new IndentedTextWriter(writer, "  ");

                indentedWriter.Write($"{type} was unhandled");
                indentedWriter.Indent++;
            }

            var properties = type.GetProperties().OrderBy(x => x.Name);
            foreach (var property in properties)
            {
                var name = property.Name;

                switch (name)
                {
                    // Ignored properties
                    case nameof(Exception.Data):
                    case nameof(Exception.TargetSite):

                    // Special case properties
                    case nameof(Exception.StackTrace):
                    case nameof(Exception.InnerException):
                        break;

                    default:
                        var value = property.GetValue(ex)?.ToString();
                        if (value != null)
                        {
                            indentedWriter.WriteLineStart($"{name}={value}");
                        }
                        break;
                }
            }

            RenderStackTrace(ex.StackTrace, indentedWriter, suppressFileInfo);

            var innerException = ex.InnerException;
            if (innerException != null)
            {
                type = innerException.GetType();
                indentedWriter.WriteLineStart($"{nameof(Exception.InnerException)}: {type}");
                indentedWriter.Indent++;
                RenderObject(rendererMap, innerException, indentedWriter, suppressFileInfo);
            }
        }

        internal static Exception FlattenException(Exception ex)
        {
            var aggr = ex as AggregateException;
            if (aggr != null)
            {
                var flattened = aggr.Flatten();
                ex = flattened.InnerExceptions.Count == 1 ?
                    flattened.InnerException :
                    flattened;
            }

            return ex;
        }

        internal static void RenderStackTrace(
            string stackTrace,
            IndentedTextWriter indentedWriter,
            bool suppressFileInfo = false,
            ILog log = null)
        {
            stackTrace = stackTrace ?? "";
            log = log ?? Log;

            var stackFrames = stackTrace.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (stackFrames.Length == 0)
                return;

            indentedWriter.WriteLineStart("StackTrace:");
            indentedWriter.Indent++;

            foreach (var stackFrame in stackFrames)
            {
                if (stackFrame.StartsWith("   at "))
                {
                    var trimmedStackFrame = stackFrame.Remove(0, 6);
                    // Stack frames from System.Runtime.CompilerServices are generally internals for handling async methods. 
                    // They produce a lot of noise in logs so we filter them out when rendering stack traces.
                    if (!trimmedStackFrame.StartsWith("System.Runtime.CompilerServices"))
                    {
                        if (suppressFileInfo)
                        {
                            var inIndex = trimmedStackFrame.IndexOf(" in ");
                            if (inIndex > -1)
                            {
                                trimmedStackFrame = trimmedStackFrame.Substring(0, inIndex);
                            }
                        }
                        indentedWriter.WriteLineStart(trimmedStackFrame);
                    }
                }
                else if (stackFrame.StartsWith("---"))
                {
                    continue;
                }
                else
                {
                    log.Warn($"Unexpected line while rendering stack trace: '{stackFrame}'.");
                }
            }

            indentedWriter.Indent--;
        }
    }
}
