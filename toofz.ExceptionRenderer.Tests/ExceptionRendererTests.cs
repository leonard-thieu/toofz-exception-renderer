using System.CodeDom.Compiler;
using System.IO;
using log4net;
using log4net.ObjectRenderer;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace toofz.Tests
{
    public class ExceptionRendererTests
    {
        public ExceptionRendererTests(ITestOutputHelper outputWriter)
        {
            this.outputWriter = outputWriter;
        }

        private readonly ITestOutputHelper outputWriter;

        public class RenderObjectMethod : ExceptionRendererTests
        {
            public RenderObjectMethod(ITestOutputHelper outputWriter) : base(outputWriter) { }

            [Fact]
            public void RendersException()
            {
                // Arrange
                var rendererMap = new RendererMap();
                var ex = ExceptionHelper.GetThrownException();
                var renderer = new ExceptionRenderer(null, suppressFileInfo: true);
                using (var sr = new StringWriter())
                {
                    // Act
                    renderer.RenderObject(rendererMap, ex, sr);
                    var output = sr.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    var expected = @"[System.Exception] Thrown test exception
  HResult=0x80131500
  Source=toofz.ExceptionRenderer.Tests
  StackTrace: 
    toofz.Tests.ExceptionHelper.GetThrownException()
    toofz.Tests.ExceptionHelper.ThrowException()";
                    Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
                }
            }

            [Fact]
            public void ExceptionHasInnerException_RendersExceptionRecursively()
            {
                // Arrange
                var rendererMap = new RendererMap();
                var ex = ExceptionHelper.GetThrownExceptionWithInnerException();
                var renderer = new ExceptionRenderer(null, suppressFileInfo: true);
                using (var sr = new StringWriter())
                {
                    // Act
                    renderer.RenderObject(rendererMap, ex, sr);
                    var output = sr.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    var expected = @"[System.Exception] Thrown test exception with inner exception
  HResult=0x80131500
  Source=toofz.ExceptionRenderer.Tests
  StackTrace: 
    toofz.Tests.ExceptionHelper.GetThrownExceptionWithInnerException()
    toofz.Tests.ExceptionHelper.ThrowExceptionWithInnerException()
  InnerException: [System.Exception] Thrown test exception
    HResult=0x80131500
    Source=toofz.ExceptionRenderer.Tests
    StackTrace: 
      toofz.Tests.ExceptionHelper.ThrowExceptionWithInnerException()
      toofz.Tests.ExceptionHelper.ThrowException()";
                    Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
                }
            }
        }

        public class RenderStackTraceMethod : ExceptionRendererTests
        {
            public RenderStackTraceMethod(ITestOutputHelper outputWriter) : base(outputWriter) { }

            [Fact]
            public void StackTraceIsNull_DoesNotThrowNullReferenceException()
            {
                // Arrange
                var renderer = new ExceptionRenderer();
                string stackTrace = null;
                using (var sw = new StringWriter())
                using (var indentedTextWriter = new IndentedTextWriter(sw))
                {
                    // Act -> Assert
                    renderer.RenderStackTrace(stackTrace, indentedTextWriter);
                }
            }

            [Fact]
            public void StackTraceIsEmpty_DoesNotRenderStackTrace()
            {
                // Arrange
                var renderer = new ExceptionRenderer();
                string stackTrace = "";
                using (var sw = new StringWriter())
                using (var indentedTextWriter = new IndentedTextWriter(sw))
                {
                    // Act
                    renderer.RenderStackTrace(stackTrace, indentedTextWriter);
                    var output = sw.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    Assert.Equal("", output);
                }
            }

            [Fact]
            public void ThrownException_RendersStackTrace()
            {
                // Arrange
                var renderer = new ExceptionRenderer(null, suppressFileInfo: true);
                var ex = ExceptionHelper.GetThrownException();
                using (var sw = new StringWriter())
                using (var indentedTextWriter = new IndentedTextWriter(sw))
                {
                    // Act
                    renderer.RenderStackTrace(ex.StackTrace, indentedTextWriter);
                    var output = sw.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    var expected = @"
StackTrace: 
    toofz.Tests.ExceptionHelper.GetThrownException()
    toofz.Tests.ExceptionHelper.ThrowException()";
                    Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
                }
            }

            [Fact]
            public void UnthrownException_RendersStackTrace()
            {
                // Arrange
                var renderer = new ExceptionRenderer(null, suppressFileInfo: true);
                var stackTraceStr = @"   at toofz.Tests.ExceptionHelper.ThrowException() in S:\Projects\toofz\toofz.Tests\ExceptionHelper.cs:line 10
   at toofz.TestsShared.Record.Exception(Action testCode) in C:\projects\toofz-testsshared\toofz.TestsShared\Record.cs:line 33";
                var ex = new UnthrownException(stackTraceStr);
                using (var sw = new StringWriter())
                using (var indentedTextWriter = new IndentedTextWriter(sw))
                {
                    // Act
                    renderer.RenderStackTrace(ex.StackTrace, indentedTextWriter);
                    var output = sw.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    var expected = @"
StackTrace: 
    toofz.TestsShared.Record.Exception(Action testCode)
    toofz.Tests.ExceptionHelper.ThrowException()";
                    Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
                }
            }

            [Fact]
            public void RendersAsyncStackFrame()
            {
                // Arrange
                var renderer = new ExceptionRenderer(null, suppressFileInfo: true);
                var ex = ExceptionHelper.GetThrownExceptionAsync();
                using (var sw = new StringWriter())
                using (var indentedTextWriter = new IndentedTextWriter(sw))
                {
                    // Act
                    renderer.RenderStackTrace(ex.StackTrace, indentedTextWriter);
                    var output = sw.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    Assert.Equal(@"
StackTrace: 
    toofz.Tests.ExceptionHelper.GetThrownExceptionAsync()
    toofz.Tests.ExceptionHelper.ThrowsExceptionAsync()
    toofz.Tests.ExceptionHelper.GetThrownException()
    toofz.Tests.ExceptionHelper.ThrowException()", output, ignoreLineEndingDifferences: true);
                }
            }

            [Fact]
            public void StackFrameStartsWith3Dashes_DoesNotLogWarning()
            {
                // Arrange
                var mockLog = new Mock<ILog>();
                var log = mockLog.Object;
                var renderer = new ExceptionRenderer(log, suppressFileInfo: true);
                var stackTraceStr = "---";
                var ex = new UnthrownException(stackTraceStr);
                using (var sw = new StringWriter())
                using (var indentedTextWriter = new IndentedTextWriter(sw))
                {
                    // Act
                    renderer.RenderStackTrace(ex.StackTrace, indentedTextWriter);
                    var output = sw.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    mockLog.Verify(l => l.Warn(It.IsAny<object>()), Times.Never);
                }
            }

            [Fact]
            public void StackFrameInWrongFormat_LogsWarning()
            {
                // Arrange
                var mockLog = new Mock<ILog>();
                var log = mockLog.Object;
                var renderer = new ExceptionRenderer(log, suppressFileInfo: true);
                var stackTraceStr = "?";
                var ex = new UnthrownException(stackTraceStr);
                using (var sw = new StringWriter())
                using (var indentedTextWriter = new IndentedTextWriter(sw))
                {
                    // Act
                    renderer.RenderStackTrace(ex.StackTrace, indentedTextWriter);
                    var output = sw.ToString();
                    outputWriter.WriteLine(output);

                    // Assert
                    mockLog.Verify(l => l.Warn(It.IsAny<object>()), Times.Once);
                }
            }
        }
    }
}
