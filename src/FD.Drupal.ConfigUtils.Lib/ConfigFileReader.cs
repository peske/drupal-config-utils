using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FD.Drupal.ConfigUtils
{
    public class ConfigFileReader : IDisposable
    {
        #region Static

        private static readonly Regex RxNonSpaceChar = new Regex(@"\S", RegexOptions.Compiled);

        private static bool IgnoreLine(string line) =>
            string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#", StringComparison.Ordinal);

        #endregion Static

        private StreamReader _reader;

        public FileInfo File { get; }

        public bool EoF { get; private set; }

        public int CurrentLineIndentSpaces { get; private set; }

        public string CurrentLineContent { get; private set; }

        public ConfigFileReader(FileInfo file)
        {
            if (!file.Exists)
                throw new FileNotFoundException(file.FullName);

            File = file;

            _reader = File.OpenText();

            NextLine();
        }

        public void NextLine()
        {
            if (_reader == null || EoF)
                return;

            string line = null;

            while (!_reader.EndOfStream)
            {
                line = _reader.ReadLine();

                if (!IgnoreLine(line))
                    break;

                line = null;
            }

            if (line == null)
            {
                CurrentLineContent = null;
                CurrentLineIndentSpaces = -1;
                EoF = true;

                return;
            }

            int index = RxNonSpaceChar.Match(line).Index;

            if (index > 0 && line.Substring(0, index).Any(c => c != ' '))
                throw new FormatException("Left indent of some of the lines contains non-space characters.");

            CurrentLineIndentSpaces = index;
            CurrentLineContent = line.Substring(index).TrimEnd();
        }

        public void Dispose()
        {
            _reader?.Dispose();

            _reader = null;
        }
    }
}