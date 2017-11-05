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
                var writer = mockWriter.Object;

                // Act
                writer.WriteLineStart(null);

                // Assert
                mockWriter.Verify(x => x.WriteLine(), Times.Once);
                mockWriter.Verify(x => x.Write((object)null), Times.Once);
            }
        }
    }
}
