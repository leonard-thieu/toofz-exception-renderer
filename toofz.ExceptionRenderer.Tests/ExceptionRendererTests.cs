using System;
using System.CodeDom.Compiler;
using System.IO;
using log4net;
using log4net.ObjectRenderer;
using Moq;
using Xunit;

namespace toofz.Tests
{
    public class ExceptionRendererTests
    {
        public class FlattenExceptionMethod
        {
            [Fact]
            public void ExIsAggregateExceptionAndHasMultipleInnerExceptions_ReturnsFlattenedException()
            {
                // Arrange
                var inner1 = new Exception();
                var inner2 = new Exception();
                var aggr = new AggregateException(inner1, inner2);

                // Act
                var ex = ExceptionRenderer.FlattenException(aggr);

                // Assert
                Assert.IsAssignableFrom<AggregateException>(ex);
                var aggr2 = (AggregateException)ex;
                Assert.True(aggr2.InnerExceptions.Count > 1);
            }

            [Fact]
            public void ExIsAggregateException_ReturnsInnerException()
            {
                // Arrange
                var inner = new Exception();
                var aggr = new AggregateException(inner);

                // Act
                var ex = ExceptionRenderer.FlattenException(aggr);

                // Assert
                Assert.Same(inner, ex);
            }

            [Fact]
            public void ExIsNotAggregateException_ReturnsEx()
            {
                // Arrange
                var ex = new Exception();

                // Act
                var ex2 = ExceptionRenderer.FlattenException(ex);

                // Assert
                Assert.Same(ex, ex2);
            }
        }

        public class RenderObjectMethod
        {
            [Fact]
            public void AggregateException_RendersFlattenedException()
            {
                // Arrange
                var rendererMap = new RendererMap();
                var inner = ExceptionHelper.GetThrownException();
                var ex = new AggregateException(inner);
                var renderer = new ExceptionRenderer(null, suppressFileInfo: true);
                using (var sr = new StringWriter())
                {
                    // Act
                    renderer.RenderObject(rendererMap, ex, sr);
                    var output = sr.ToString();

                    // Assert
                    var expected = @"[System.Exception] Thrown test exception
  HResult=-2146233088
  Source=toofz.ExceptionRenderer.Tests
  StackTrace: 
    toofz.Tests.ExceptionHelper.ThrowException()
    toofz.Tests.ExceptionHelper.GetThrownException()";
                    Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
                }
            }

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

                    // Assert
                    var expected = @"[System.Exception] Thrown test exception
  HResult=-2146233088
  Source=toofz.ExceptionRenderer.Tests
  StackTrace: 
    toofz.Tests.ExceptionHelper.ThrowException()
    toofz.Tests.ExceptionHelper.GetThrownException()";
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

                    // Assert
                    var expected = @"[System.Exception] Thrown test exception with inner exception
  HResult=-2146233088
  Source=toofz.ExceptionRenderer.Tests
  StackTrace: 
    toofz.Tests.ExceptionHelper.ThrowExceptionWithInnerException()
    toofz.Tests.ExceptionHelper.GetThrownExceptionWithInnerException()
  InnerException: [System.Exception] Thrown test exception
    HResult=-2146233088
    Source=toofz.ExceptionRenderer.Tests
    StackTrace: 
      toofz.Tests.ExceptionHelper.ThrowException()
      toofz.Tests.ExceptionHelper.ThrowExceptionWithInnerException()";
                    Assert.Equal(expected, output, ignoreLineEndingDifferences: true);
                }
            }
        }

        public class RenderStackTraceMethod
        {
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

                    // Assert
                    Assert.Equal("", sw.ToString());
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

                    // Assert
                    var expected = @"
StackTrace: 
    toofz.Tests.ExceptionHelper.ThrowException()
    toofz.Tests.ExceptionHelper.GetThrownException()";
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

                    // Assert
                    var expected = @"
StackTrace: 
    toofz.Tests.ExceptionHelper.ThrowException()
    toofz.TestsShared.Record.Exception(Action testCode)";
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

                    // Assert
                    Assert.Equal(@"
StackTrace: 
    toofz.Tests.ExceptionHelper.ThrowException()
    toofz.Tests.ExceptionHelper.GetThrownException()
    toofz.Tests.ExceptionHelper.ThrowsExceptionAsync()
    toofz.Tests.ExceptionHelper.GetThrownExceptionAsync()", output, ignoreLineEndingDifferences: true);
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

                    // Assert
                    mockLog.Verify(l => l.Warn(It.IsAny<object>()), Times.Once);
                }
            }
        }
    }
}
