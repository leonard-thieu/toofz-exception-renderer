using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Initializes an instance of the <see cref="ExceptionRenderer"/> class.
        /// </summary>
        public ExceptionRenderer() : this(null, suppressFileInfo: false) { }

        /// <summary>
        /// Initializes an instance of the <see cref="ExceptionRenderer"/> class.
        /// </summary>
        /// <param name="log">Logger</param>
        /// <param name="suppressFileInfo">
        /// Suppresses file information in stack traces. This is used for repeatable tests.
        /// </param>
        internal ExceptionRenderer(ILog log, bool suppressFileInfo)
        {
            this.log = log ?? Log;
            this.suppressFileInfo = suppressFileInfo;
        }

        private readonly ILog log;
        private readonly bool suppressFileInfo;

        /// <summary>
        /// Renders an object of type <see cref="Exception"/> similar to how Visual Studio Exception Assistant 
        /// renders it.
        /// </summary>
        /// <param name="rendererMap">Not used.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="writer">The writer.</param>
        public void RenderObject(RendererMap rendererMap, object exception, TextWriter writer)
        {
            var ex = FlattenException((Exception)exception);
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
                        var value = property.GetValue(ex);
                        if (value != null)
                        {
                            indentedWriter.WriteLineStart($"{name}=");
                            rendererMap.FindAndRender(value, indentedWriter);
                        }
                        break;
                }
            }

            RenderStackTrace(ex.StackTrace, indentedWriter);

            var innerException = ex.InnerException;
            if (innerException != null)
            {
                type = innerException.GetType();
                indentedWriter.WriteLineStart($"{nameof(Exception.InnerException)}: {type}");
                indentedWriter.Indent++;
                RenderObject(rendererMap, innerException, indentedWriter);
            }
        }

        internal void RenderStackTrace(
            string stackTrace,
            IndentedTextWriter indentedWriter)
        {
            stackTrace = stackTrace ?? "";

            var stackFrames = stackTrace.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (stackFrames.Length == 0) { return; }

            indentedWriter.WriteLineStart("StackTrace:");
            indentedWriter.Indent++;

            foreach (var stackFrame in stackFrames)
            {
                if (stackFrame.StartsWith("   at "))
                {
                    var trimmedStackFrame = stackFrame.Substring(6);
                    // Stack frames from the following namespaces are generally internals for handling async methods. 
                    // Filtering them out reduces noise when rendering stack traces.
                    if (trimmedStackFrame.StartsWith("System.Runtime.CompilerServices")) { continue; }
                    if (trimmedStackFrame.StartsWith("System.Runtime.ExceptionServices")) { continue; }

                    if (suppressFileInfo)
                    {
                        var inIndex = trimmedStackFrame.IndexOf(" in ");
                        if (inIndex > -1)
                        {
                            trimmedStackFrame = trimmedStackFrame.Remove(inIndex);
                        }
                    }

                    // Strip off compiler-generated types
                    var displayClassRegex = new Regex(@"(?:<>\w__DisplayClass\d+_\d+(?:`\d+)?\.)", RegexOptions.None, TimeSpan.FromSeconds(5));
                    trimmedStackFrame = displayClassRegex.Replace(trimmedStackFrame, "");
                    var asyncRegex = new Regex(@"<?<(\w+)>\w__\d+(?:`\d+)?>?\w?(?:\.MoveNext)?", RegexOptions.None, TimeSpan.FromSeconds(5));
                    trimmedStackFrame = asyncRegex.Replace(trimmedStackFrame, "$1");

                    indentedWriter.WriteLineStart(trimmedStackFrame);
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
