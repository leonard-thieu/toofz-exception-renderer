﻿using System;
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
                    case nameof(Exception.Message):
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
                indentedWriter.WriteLineStart($"{nameof(Exception.InnerException)}: ");
                // Calling RendererMap.FindAndRender(object obj, TextWriter writer) ends up using the default Exception renderer
                // instead of this one ¯\_(ツ)_/¯
                RenderObject(rendererMap, innerException, indentedWriter);
            }
        }

        internal void RenderStackTrace(
            string stackTrace,
            IndentedTextWriter indentedWriter)
        {
            stackTrace = stackTrace ?? "";

            var stackFrames = stackTrace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (stackFrames.Length == 0) { return; }

            indentedWriter.WriteLineStart($"{nameof(Exception.StackTrace)}: ");
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

                    var inIndex = trimmedStackFrame.IndexOf(" in ");
                    if (inIndex > -1)
                    {
                        if (suppressFileInfo)
                        {
                            trimmedStackFrame = trimmedStackFrame.Remove(inIndex);
                        }
                        // Reduce noise for projects built on AppVeyor by stripping off the root build directory.
                        else
                        {
                            var fileInfoIndex = inIndex + " in ".Length;
                            var fileInfo = trimmedStackFrame.Substring(fileInfoIndex).Split(new[] { ":line " }, StringSplitOptions.None);
                            var filePath = fileInfo[0];
                            var lineNumber = fileInfo[1];
                            // Probably built on AppVeyor
                            if (filePath.StartsWith(@"C:\projects\"))
                            {
                                var solutionDirIndex = filePath.IndexOf('\\', @"C:\projects\".Length);
                                if (solutionDirIndex > -1)
                                {
                                    trimmedStackFrame = trimmedStackFrame.Remove(fileInfoIndex) +
                                        filePath.Substring(solutionDirIndex + 1) + ":line " + lineNumber;
                                }
                            }
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
