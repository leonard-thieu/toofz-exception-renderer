﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            var ex = (Exception)exception;
            var type = ex.GetType();

            var indentedWriter = writer as IndentedTextWriter;
            if (indentedWriter == null)
            {
                indentedWriter = new IndentedTextWriter(writer, "  ");
            }

            indentedWriter.Write($"[{type}] {ex.Message}");
            indentedWriter.Indent++;

            foreach (var property in type.GetProperties().OrderBy(x => x.Name))
            {
                var name = property.Name;

                switch (name)
                {
                    // Ignored properties
                    case nameof(Exception.Data):
                    case nameof(Exception.TargetSite):

                    // Special case properties
                    case nameof(Exception.Message):
                    case nameof(Exception.StackTrace):
                    case nameof(Exception.InnerException):
                        break;

                    default:
                        var value = property.GetValue(ex);
                        if ((name == nameof(Exception.HResult)) ||
                            (ex is ExternalException && name == nameof(ExternalException.ErrorCode)))
                        {
                            indentedWriter.WriteLineStart($"{name}=0x{value:X}");
                        }
                        else if (value != null)
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
                indentedWriter.WriteLineStart($"{nameof(Exception.InnerException)}: ");
                // Calling RendererMap.FindAndRender(object obj, TextWriter writer) ends up using the default Exception renderer
                // instead of this one ¯\_(ツ)_/¯
                RenderObject(rendererMap, innerException, indentedWriter);
            }

            indentedWriter.Indent--;
        }

        internal void RenderStackTrace(
            string stackTrace,
            IndentedTextWriter indentedWriter)
        {
            const string StackFramePrefix = "   at ";

            stackTrace = stackTrace ?? "";

            var stackFrames = stackTrace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (stackFrames.Length == 0) { return; }

            indentedWriter.WriteLineStart($"{nameof(Exception.StackTrace)}: ");
            indentedWriter.Indent++;

            foreach (var stackFrame in stackFrames.Reverse())
            {
                if (stackFrame.StartsWith(StackFramePrefix))
                {
                    var trimmedStackFrame = stackFrame.Substring(StackFramePrefix.Length);
                    // Stack frames from the following namespaces are generally internals for handling async methods. 
                    // Filtering them out reduces noise when rendering stack traces.
                    if (trimmedStackFrame.StartsWith("System.Runtime.CompilerServices")) { continue; }
                    if (trimmedStackFrame.StartsWith("System.Runtime.ExceptionServices")) { continue; }

                    var esf = new ExceptionStackFrame(trimmedStackFrame);

                    indentedWriter.WriteLineStart(esf.ToString(suppressFileInfo));
                }
                // --- End of stack trace from previous location where exception was thrown ---
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
