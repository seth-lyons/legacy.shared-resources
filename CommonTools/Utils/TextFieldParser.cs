using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SharedResources
{
    public class TextFieldParser : IDisposable
    {
        private string[] _delimiters;
        private bool _disposed;
        private TextReader _reader;
        private Regex _delimiterRegex;
        private Regex _beginQuotesRegex;
        private Regex _delimiterWithEndCharsRegex;
        private string _spaceChars;
        private int _position = 0;
        private int _peekPosition = 0;
        private int _charsRead = 0;
        private bool _trimWhiteSpace = true;
        private bool _endOfData = false;
        private long _lineNumber = (long)1;
        private string _errorLine = "";
        private long _errorLineNumber = (long)-1;
        private const RegexOptions REGEX_OPTIONS = RegexOptions.CultureInvariant;
        private int[] _whitespaceCodes = new int[] { 0x9, 0xB, 0xC, 0x20, 0x85, 0xA0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A, 0x200B, 0x2028, 0x2029, 0x3000, 0xFEFF };
        private const int DEFAULT_BUFFER_LENGTH = 4096;
        private const int DEFAULT_BUILDER_INCREASE = 10;
        private char[] _buffer = new char[4096];
        private bool _hasFieldsEnclosedInQuotes = true;
        private int _maxLineSize = 10000000;
        private int _maxBufferSize = 10000000;
        private const string BEGINS_WITH_QUOTE = @"\G[{0}]*""";
        private const string ENDING_QUOTE = "\"[{0}]*";

        public TextFieldParser(Stream stream, string[] delimiters = null, Encoding defaultEncoding = null, bool detectEncoding = true)
        {
            _delimiters = delimiters ?? new[] { "," };
            ValidateDelimiters(_delimiters);
            ValidateAndEscapeDelimiters();
            InitializeFromStream(stream, defaultEncoding ?? Encoding.UTF8, detectEncoding);
        }

        private void InitializeFromStream(Stream stream, Encoding defaultEncoding, bool detectEncoding)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("", nameof(stream));

            _reader = new StreamReader(stream, defaultEncoding ?? Encoding.Default, detectEncoding);
            ReadToBuffer();
        }

        private int ReadToBuffer()
        {
            _position = 0;
            int BufferLength = _buffer.Length;

            if (BufferLength > DEFAULT_BUFFER_LENGTH)
            {
                BufferLength = DEFAULT_BUFFER_LENGTH;
                _buffer = new char[BufferLength - 1 + 1];
            }

            _charsRead = _reader.Read(_buffer, 0, BufferLength);

            return _charsRead;
        }

        public string[] ReadFields()
        {
            if (_reader == null | _buffer == null)
                return null;

            return ParseDelimitedLine();
        }


        private string[] ParseDelimitedLine()
        {
            string Line = ReadNextDataLine();
            if (Line == null)
                return null;

            long CurrentLineNumber = _lineNumber - (long)1;

            int Index = 0;
            System.Collections.Generic.List<string> Fields = new System.Collections.Generic.List<string>();
            string Field;
            int LineEndIndex = GetEndOfLineIndex(Line);

            while (Index <= LineEndIndex)
            {
                Match MatchResult = null;
                bool QuoteDelimited = false;

                if (_hasFieldsEnclosedInQuotes)
                {
                    MatchResult = BeginQuotesRegex.Match(Line, Index);
                    QuoteDelimited = MatchResult.Success;
                }

                if (QuoteDelimited)
                {
                    Index = MatchResult.Index + MatchResult.Length;
                    QuoteDelimitedFieldBuilder EndHelper = new QuoteDelimitedFieldBuilder(_delimiterWithEndCharsRegex, _spaceChars);
                    EndHelper.BuildField(Line, Index);

                    if (EndHelper.MalformedLine)
                    {
                        _errorLine = Line.TrimEnd((char)13, (char)10);
                        _errorLineNumber = CurrentLineNumber;
                        throw new FormatException();
                    }

                    if (EndHelper.FieldFinished)
                    {
                        Field = EndHelper.Field;
                        Index = EndHelper.Index + EndHelper.DelimiterLength;
                    }
                    else
                    {
                        string NewLine;
                        int EndOfLine;

                        do
                        {
                            EndOfLine = Line.Length;
                            NewLine = ReadNextDataLine();

                            if (NewLine == null)
                            {
                                _errorLine = Line.TrimEnd((char)13, (char)10);
                                _errorLineNumber = CurrentLineNumber;
                                throw new FormatException();
                            }

                            if ((Line.Length + NewLine.Length) > _maxLineSize)
                            {
                                _errorLine = Line.TrimEnd((char)13, (char)10);
                                _errorLineNumber = CurrentLineNumber;
                                throw new FormatException();
                            }

                            Line += NewLine;
                            LineEndIndex = GetEndOfLineIndex(Line);
                            EndHelper.BuildField(Line, EndOfLine);
                            if (EndHelper.MalformedLine)
                            {
                                _errorLine = Line.TrimEnd((char)13, (char)10);
                                _errorLineNumber = CurrentLineNumber;
                                throw new FormatException();
                            }
                        }
                        while (!EndHelper.FieldFinished);

                        Field = EndHelper.Field;
                        Index = EndHelper.Index + EndHelper.DelimiterLength;
                    }

                    if (_trimWhiteSpace)
                        Field = Field.Trim();

                    Fields.Add(Field);
                }
                else
                {
                    Match DelimiterMatch = _delimiterRegex.Match(Line, Index);
                    if (DelimiterMatch.Success)
                    {
                        Field = Line.Substring(Index, DelimiterMatch.Index - Index);

                        if (_trimWhiteSpace)
                            Field = Field.Trim();
                        Fields.Add(Field);
                        Index = DelimiterMatch.Index + DelimiterMatch.Length;
                    }
                    else
                    {
                        Field = Line.Substring(Index).TrimEnd((char)13, (char)10);

                        if (_trimWhiteSpace)
                            Field = Field.Trim();
                        Fields.Add(Field);
                        break;
                    }
                }
            }

            return Fields.ToArray();
        }

        private delegate int ChangeBufferFunction();
        private string ReadNextDataLine()
        {
            string Line;
            ChangeBufferFunction BufferFunction = new ChangeBufferFunction(ReadToBuffer);
            do
            {
                Line = ReadNextLine(ref _position, BufferFunction);
                _lineNumber += 1;
            }
            while (IgnoreLine(Line));

            if (Line == null)
                CloseReader();

            return Line;
        }

        private string ReadNextLine(ref int Cursor, ChangeBufferFunction ChangeBuffer)
        {
            if (Cursor == _charsRead && ChangeBuffer() == 0)
                return null;

            StringBuilder Builder = null;
            do
            {
                var loopTo = _charsRead - 1;
                for (int i = Cursor; i <= loopTo; i++)
                {
                    char Character = _buffer[i];
                    if (Character == '\r' || Character == '\n')
                    {
                        if (Builder != null)
                            Builder.Append(_buffer, Cursor, (i - Cursor) + 1);
                        else
                        {
                            Builder = new StringBuilder(i + 1);
                            Builder.Append(_buffer, Cursor, (i - Cursor) + 1);
                        }

                        Cursor = i + 1;
                        if (Character == '\r')
                        {
                            if (Cursor < _charsRead)
                            {
                                if (_buffer[Cursor] == '\n')
                                {
                                    Cursor += 1;
                                    Builder.Append('\n');
                                }
                            }
                            else if (ChangeBuffer() > 0)
                            {
                                if (_buffer[Cursor] == '\n')
                                {
                                    Cursor += 1;
                                    Builder.Append('\n');
                                }
                            }
                        }
                        return Builder.ToString();
                    }
                }

                int Size = _charsRead - Cursor;
                if (Builder == null)
                    Builder = new StringBuilder(Size + DEFAULT_BUILDER_INCREASE);
                Builder.Append(_buffer, Cursor, Size);
            }
            while (ChangeBuffer() > 0);

            return Builder.ToString();
        }

        public bool EndOfData
        {
            get
            {
                if (_endOfData)
                    return _endOfData;

                if (_reader == null | _buffer == null)
                {
                    _endOfData = true;
                    return true;
                }

                if (PeekNextDataLine() != null)
                    return false;

                _endOfData = true;
                return true;
            }
        }

        public void Close()
        {
            CloseReader();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                    Close();
                _disposed = true;
            }
        }

        ~TextFieldParser()
        {
            Dispose(false);
        }

        private void CloseReader()
        {
            FinishReading();
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }

        private void FinishReading()
        {
            _lineNumber = -1;
            _endOfData = true;
            _buffer = null;
            _delimiterRegex = null;
            _beginQuotesRegex = null;
        }

        private bool IgnoreLine(string line)
        {
            if (line == null) return false;

            string TrimmedLine = line.Trim();
            if (TrimmedLine.Length == 0)
                return true;
            return false;
        }

        private int SlideCursorToStartOfBuffer()
        {
            if (_position > 0)
            {
                int BufferLength = _buffer.Length;
                char[] TempArray = new char[BufferLength - 1 + 1];
                Array.Copy(_buffer, _position, TempArray, 0, BufferLength - _position);

                int CharsRead = _reader.Read(TempArray, BufferLength - _position, _position);
                _charsRead = (_charsRead - _position) + CharsRead;

                _position = 0;
                _buffer = TempArray;

                return CharsRead;
            }

            return 0;
        }

        private int IncreaseBufferSize()
        {
            _peekPosition = _charsRead;
            int BufferSize = _buffer.Length + DEFAULT_BUFFER_LENGTH;

            if (BufferSize > _maxBufferSize)
                throw new InvalidOperationException();

            char[] TempArray = new char[BufferSize - 1 + 1];

            Array.Copy(_buffer, TempArray, _buffer.Length);
            int CharsRead = _reader.Read(TempArray, _buffer.Length, DEFAULT_BUFFER_LENGTH);
            _buffer = TempArray;
            _charsRead += CharsRead;

            return CharsRead;
        }

        private string PeekNextDataLine()
        {
            string Line;
            ChangeBufferFunction BufferFunction = new ChangeBufferFunction(IncreaseBufferSize);
            SlideCursorToStartOfBuffer();
            _peekPosition = 0;

            do Line = ReadNextLine(ref _peekPosition, BufferFunction);
            while (IgnoreLine(Line));

            return Line;
        }

        private int GetEndOfLineIndex(string Line)
        {
            int Length = Line.Length;

            if (Length == 1)
                return Length;

            if (Line[Length - 2] == '\r' | Line[Length - 2] == '\n')
                return Length - 2;
            else if (Line[Length - 1] == '\r' | Line[Length - 1] == '\n')
                return Length - 1;
            else
                return Length;
        }

        private void ValidateAndEscapeDelimiters()
        {
            if (_delimiters == null)
                throw new ArgumentException("", nameof(_delimiters));

            if (_delimiters.Length == 0)
                throw new ArgumentException("", nameof(_delimiters));

            int Length = _delimiters.Length;

            StringBuilder Builder = new StringBuilder();
            StringBuilder QuoteBuilder = new StringBuilder();

            QuoteBuilder.Append(EndQuotePattern + "(");
            var loopTo = Length - 1;
            for (int i = 0; i <= loopTo; i++)
            {
                if (_delimiters[i] != null)
                {
                    if (_hasFieldsEnclosedInQuotes)
                    {
                        if (_delimiters[i].IndexOf('"') > -1)
                            throw new InvalidOperationException();
                    }

                    string EscapedDelimiter = Regex.Escape(_delimiters[i]);

                    Builder.Append(EscapedDelimiter + "|");
                    QuoteBuilder.Append(EscapedDelimiter + "|");
                }
                else
                    Console.WriteLine("Delimiter element is empty. This should have been caught on input");
            }

            _spaceChars = WhitespaceCharacters;

            _delimiterRegex = new Regex(Builder.ToString(0, Builder.Length - 1), REGEX_OPTIONS);
            Builder.Append("\r|\n");
            _delimiterWithEndCharsRegex = new Regex(Builder.ToString(), REGEX_OPTIONS);
            QuoteBuilder.Append("\r|\n)|\"$");
        }

        private void ValidateDelimiters(string[] delimiterArray)
        {
            if (delimiterArray == null)
                return;
            foreach (string delimiter in delimiterArray)
            {
                if (string.IsNullOrEmpty(delimiter))
                    throw new ArgumentException("", nameof(_delimiters));
                if (delimiter.IndexOfAny(new char[] { (char)13, (char)10 }) > -1)
                    throw new ArgumentException("", nameof(_delimiters));
            }
        }

        private Regex BeginQuotesRegex
        {
            get
            {
                if (_beginQuotesRegex == null)
                {
                    string pattern = string.Format(CultureInfo.InvariantCulture, BEGINS_WITH_QUOTE, WhitespacePattern);
                    _beginQuotesRegex = new Regex(pattern, REGEX_OPTIONS);
                }

                return _beginQuotesRegex;
            }
        }

        private string EndQuotePattern =>string.Format(CultureInfo.InvariantCulture, ENDING_QUOTE, WhitespacePattern);

        private string WhitespaceCharacters
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (int code in _whitespaceCodes)
                {
                    char spaceChar = (char)code;
                    if (!CharacterIsInDelimiter(spaceChar))
                        builder.Append(spaceChar);
                }

                return builder.ToString();
            }
        }

        private string WhitespacePattern
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (int code in _whitespaceCodes)
                {
                    char spaceChar = (char)code;
                    if (!CharacterIsInDelimiter(spaceChar))
                        builder.Append(@"\u" + code.ToString("X4", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private bool CharacterIsInDelimiter(char testCharacter)
        {
            foreach (string delimiter in _delimiters)
            {
                if (delimiter.IndexOf(testCharacter) > -1)
                    return true;
            }
            return false;
        }
    }

    internal class QuoteDelimitedFieldBuilder
    {
        private StringBuilder _field = new StringBuilder();
        private bool _fieldFinished;
        private int _index;
        private int _delimiterLength;
        private Regex _delimiterRegex;
        private string _spaceChars;
        private bool _malformedLine;

        public QuoteDelimitedFieldBuilder(Regex DelimiterRegex, string SpaceChars)
        {
            _delimiterRegex = DelimiterRegex;
            _spaceChars = SpaceChars;
        }

        public bool FieldFinished => _fieldFinished;
        public string Field => _field.ToString();
        public int Index => _index;
        public int DelimiterLength => _delimiterLength;
        public bool MalformedLine => _malformedLine;

        public void BuildField(string Line, int StartAt)
        {
            _index = StartAt;
            int Length = Line.Length;

            while (_index < Length)
            {
                if (Line[_index] == '"')
                {
                    if ((_index + 1) == Length)
                    {
                        _fieldFinished = true;
                        _delimiterLength = 1;

                        _index += 1;
                        return;
                    }
                    if (((_index + 1) < Line.Length) & (Line[_index + 1] == '"'))
                    {
                        _field.Append('"');
                        _index += 2;
                        continue;
                    }

                    int Limit;
                    Match DelimiterMatch = _delimiterRegex.Match(Line, _index + 1);
                    if (!DelimiterMatch.Success)
                        Limit = Length - 1;
                    else
                        Limit = DelimiterMatch.Index - 1;

                    var loopTo = Limit;
                    for (int i = _index + 1; i <= loopTo; i++)
                    {
                        if (_spaceChars.IndexOf(Line[i]) < 0)
                        {
                            _malformedLine = true;
                            return;
                        }
                    }

                    _delimiterLength = (1 + Limit) - _index;
                    if (DelimiterMatch.Success)
                        _delimiterLength += DelimiterMatch.Length;

                    _fieldFinished = true;
                    return;
                }
                else
                {
                    _field.Append(Line[_index]);
                    _index += 1;
                }
            }
        }
    }
}