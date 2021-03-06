﻿using System.IO;

namespace toofz
{
    /// <summary>
    /// Contains extension methods for <see cref="TextWriter"/>.
    /// </summary>
    internal static class TextWriterExtensions
    {
        /// <summary>
        /// Writes a line terminator, followed by the text representation of an object by calling the ToString method on 
        /// that object to the text string or stream.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteLineStart(this TextWriter writer, object value)
        {
            writer.WriteLine();
            writer.Write(value);
        }
    }
}
