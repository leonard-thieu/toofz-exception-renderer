using System;
using System.Text.RegularExpressions;

namespace toofz
{
    internal sealed class ExceptionStackFrame
    {
        private const string FileInfoPrefix = " in ";
        private const string FileInfoLinePrefix = ":line ";

        public ExceptionStackFrame(string stackFrame)
        {
            if (stackFrame.TryIndexOf(FileInfoPrefix, out var fileInfoPrefixIndex))
            {
                Method = stackFrame.Remove(fileInfoPrefixIndex);

                var fileInfoIndex = fileInfoPrefixIndex + FileInfoPrefix.Length;
                var fileInfo = stackFrame.Substring(fileInfoIndex).Split(new[] { FileInfoLinePrefix }, StringSplitOptions.None);
                FilePath = fileInfo[0];
                LineNumber = int.Parse(fileInfo[1]);
            }
            else
            {
                Method = stackFrame;
            }
        }

        #region Method

        private static readonly Regex DisplayClassRegex = new Regex(@"(?:<>\w__DisplayClass\w+(?:_\d+)?(?:`\d+)?\.)", RegexOptions.None, TimeSpan.FromSeconds(5));
        private static readonly Regex AsyncRegex = new Regex(@"<?<(\w+)>\w__\d+(?:`\d+)?>?\w?(?:\.MoveNext)?", RegexOptions.None, TimeSpan.FromSeconds(5));

        public string Method
        {
            get { return method; }
            private set
            {
                // Strip off compiler-generated types and methods
                value = DisplayClassRegex.Replace(value, "");
                value = AsyncRegex.Replace(value, "$1");

                method = value;
            }
        }
        private string method;

        #endregion

        #region FilePath

        private const string AppVeyorCommonPath = @"C:\projects\";

        public string FilePath
        {
            get { return filePath; }
            set
            {
                // Reduce noise for projects built on AppVeyor by stripping off the build directory.
                if (value.StartsWith(AppVeyorCommonPath))
                {
                    // Get the build directory.
                    // C:\projects\toofz-exception-renderer\
                    //                                     ^
                    if (value.TryIndexOf('\\', AppVeyorCommonPath.Length, out var solutionDirIndex))
                    {
                        value = value.Substring(solutionDirIndex + 1);
                    }
                }

                filePath = value;
            }
        }
        private string filePath;

        #endregion

        public int LineNumber { get; }

        public string ToString(bool suppressFileInfo)
        {
            if (suppressFileInfo) { return ToString(); }

            var str = $"{ToString()}{FileInfoPrefix}";
            if (LineNumber > 0)
            {
                str += $"{FileInfoLinePrefix}{LineNumber}";
            }

            return str;
        }

        public override string ToString()
        {
            return Method;
        }
    }
}
