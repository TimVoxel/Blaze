using Blaze.SyntaxTokens;
using Blaze.Diagnostics;
using Blaze.Text;
using System.Text;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze
{
    internal class Lexer
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private ImmutableArray<Trivia>.Builder _triviaBuilder = ImmutableArray.CreateBuilder<Trivia>();
        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object? _value;
        
        private char Current => Peek(0);
        private char Next => Peek(1);

        public DiagnosticBag Diagnostics => _diagnostics;

        public Lexer(SyntaxTree syntaxTree)
        {
            _text = syntaxTree.Text;
            _syntaxTree = syntaxTree;
        }

        public SyntaxToken Lex()
        {
            ReadTrivia(true);
            var leadingTrivia = _triviaBuilder.ToImmutable();

            var tokenStart = _position;

            ReadToken();

            var tokenKind = _kind;
            var tokenValue = _value;
            var tokenLength = _position - _start;

            ReadTrivia(false);

            var trailingTrivia = _triviaBuilder.ToImmutable();
            var tokenText = SyntaxFacts.GetText(tokenKind);
            if (tokenText == null)
                tokenText = _text.ToString(tokenStart, tokenLength);

            return new SyntaxToken(_syntaxTree, tokenKind, tokenStart, tokenText, tokenValue, leadingTrivia, trailingTrivia);
        }

        private void ReadToken()
        {
            _start = _position;
            _kind = SyntaxKind.IncorrectToken;
            _value = null;

            switch (Current)
            {
                case '\0':
                    _kind = SyntaxKind.EndOfFileToken;
                    break;
                case ';':
                    ConsumeOfKind(SyntaxKind.SemicolonToken);
                    break;
                case ':':
                    ConsumeOfKind(SyntaxKind.ColonToken);
                    break;
                case ',':
                    ConsumeOfKind(SyntaxKind.CommaToken);
                    break;
                case '+':
                    ConsumeOfKind(SyntaxKind.PlusToken);
                    break;
                case '-':
                    ConsumeOfKind(SyntaxKind.MinusToken);
                    break;
                case '*':
                    ConsumeOfKind(SyntaxKind.StarToken);
                    break;
                case '/':
                    ConsumeOfKind(SyntaxKind.SlashToken);
                    break;
                case '(':
                    ConsumeOfKind(SyntaxKind.OpenParenToken);
                    break;
                case ')':
                    ConsumeOfKind(SyntaxKind.CloseParenToken);
                    break;
                case '{':
                    ConsumeOfKind(SyntaxKind.OpenBraceToken);
                    break;
                case '}':
                    ConsumeOfKind(SyntaxKind.CloseBraceToken);
                    break;
                case '!':
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.NotEqualsToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.ExclamationSignToken);
                    break;
                case '&':
                    if (Next == '&')
                    {
                        _kind = SyntaxKind.DoubleAmpersandToken;
                        _position += 2;
                    }
                    else
                        ConsumeStray();
                    break;
                case '|':
                    if (Next == '|')
                    {
                        _kind = SyntaxKind.DoublePipeToken;
                        _position += 2;
                    }
                    else
                        ConsumeStray();
                    break;
                case '.':
                    if (Next == '.')
                    {
                        _kind = SyntaxKind.DoubleDotToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.DotToken);
                    break;
                case '<':
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.LessOrEqualsToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.LessToken);
                    break;
                case '>':
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.GreaterOrEqualsToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.GreaterToken);
                    break;
                case '=':
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.DoubleEqualsToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.EqualsToken);
                    break;
                case '"':
                    ReadStringLiteral();
                    break;
                default:
                    if (char.IsDigit(Current))
                    {
                        ReadIntegerLiteral();
                    }
                    else if (char.IsLetter(Current) || Current == '_')
                    {
                        ReadIdentifierOrKeyword();
                    }
                    else
                        ConsumeStray();
                    break;
            }
        }

        private void ReadTrivia(bool isLeading)
        {
            _triviaBuilder.Clear();
            var done = false;
            while (!done)
            {
                _start = _position;
                _kind = SyntaxKind.IncorrectToken;
                _value = null;

                switch (Current)
                {
                    case '\0':
                        done = true;
                        break;
                    case '/':
                        if (Next == '/')
                            ReadSingleLineComment();
                        else if (Next == '*')
                            ReadMultiLineComment();
                        else
                            done = true;
                        break;
                    case '\r':
                    case '\n':
                        if (!isLeading)
                            done = true;
                        ReadLineBreak();
                        break;
                    case '\t':
                    case ' ':
                        ReadWhitespace();
                        break;
                    default:
                        if (char.IsWhiteSpace(Current))
                            ReadWhitespace();
                        else
                            done = true;
                        break;
                }

                var length = _position - _start;
                if (length > 0)
                {
                    var text = _text.ToString(_start, length);
                    var trivia = new Trivia(_syntaxTree, _kind, _start, text);
                    _triviaBuilder.Add(trivia);
                }
            }
        }

        private void ConsumeStray()
        {
            var span = new TextSpan(_position, 1);
            var location = new TextLocation(_text, span);
            _diagnostics.ReportStrayCharacter(location, Current);
            _position++;
        }

        private void ConsumeOfKind(SyntaxKind kind)
        {
            _kind = kind;
            _position++;
        }

        private char Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _text.Length)
                return '\0';
            return _text[index];
        }

        private void ReadIntegerLiteral()
        {
            while (char.IsDigit(Current))
                _position++;

            int length = _position - _start;
            string text = _text.ToString(_start, length);
            if (!int.TryParse(text, out int value))
            {
                TextSpan span = new TextSpan(_start, length);
                TextLocation location = new TextLocation(_text, span);
                _diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Int);
            }
                
            _value = value;
            _kind = SyntaxKind.IntegerLiteralToken;
        }

        private void ReadWhitespace()
        {
            bool done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\r':
                    case '\n':
                    case '\0':
                        done = true;
                        break;
                    default:
                        if (!char.IsWhiteSpace(Current))
                            done = true;
                        else
                            _position++;
                        break;
                }
            }
            _kind = SyntaxKind.WhitespaceTrivia;
        }

        private void ReadLineBreak()
        {
            if (Current == '\r' && Next == '\n')
            {
                _position += 2;
            }
            else
                _position++;

            _kind = SyntaxKind.LineBreakTrivia;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (char.IsLetterOrDigit(Current) || Current == '_')
                _position++;
            int length = _position - _start;
            string text = _text.ToString(_start, length);
            _kind = SyntaxFacts.GetKeywordKind(text);
        }

        private void ReadStringLiteral()
        {
            _position++;
            StringBuilder value = new StringBuilder();

            bool done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        TextSpan span = new TextSpan(_start, 1);
                        TextLocation location = new TextLocation(_text, span);
                        _diagnostics.ReportUnterminatedString(location);
                        done = true;
                        break;
                    case '"':
                        _position++;
                        done = true;
                        break;
                    case '\\':
                        if (Next == '"' || Next == '\\')
                        {
                            value.Append(Next);
                            _position += 2;
                        }
                        else
                            _position++;
                        break;
                    default:
                        value.Append(Current);
                        _position++;
                        break;
                }
            }

            _kind = SyntaxKind.StringLiteralToken;
            _value = value.ToString();
        }

        private void ReadSingleLineComment()
        {
            _position += 2;
            bool done = false;
            _kind = SyntaxKind.SingleLineCommentTrivia;

            while (!done)
            {
                switch (Current)
                {
                    case '\r':
                    case '\n':
                    case '\0':    
                        done = true;
                        break;
                    default:
                        _position++;
                        break;     
                }
            }
        }

        private void ReadMultiLineComment()
        {
            _kind = SyntaxKind.MultiLineCommentTrivia;
            _position += 2;
            bool done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                        TextSpan span = new TextSpan(_start, 2);
                        TextLocation location = new TextLocation(_text, span);
                        _diagnostics.ReportUnterminatedMultiLineComment(location);
                        done = true;
                        break;
                    case '*':
                        if (Next == '/')
                        {
                            _position++;
                            done = true;
                        }
                        _position++;
                        break;
                    default:
                        _position++;
                        break;
                }
            }

        }
    }
}
