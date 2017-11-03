using System;
using System.IO;
using Moq;
using Xunit;

namespace toofz.Tests
{
    public class TextWriterExtensionsTests
    {
        public class WriteLineStart
        {
            [Fact]
            public void WritesLineThenValue()
            {
                // Arrange
                var mockWriter = new Mock<TextWriter>();

                // Act
                TextWriterExtensions.WriteLineStart(mockWriter.Object, null);

                // Assert
                mockWriter.Verify(x => x.WriteLine(), Times.Once);
                mockWriter.Verify(x => x.Write((object)null), Times.Once);
            }

            [Fact]
            public void WriterIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                TextWriter writer = null;

                // Act -> Assert
                Assert.Throws<ArgumentNullException>(() =>
                {
                    TextWriterExtensions.WriteLineStart(writer, null);
                });
            }
        }
    }
}
