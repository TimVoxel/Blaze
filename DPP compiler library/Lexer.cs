using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Diagnostics;
using DPP_Compiler.Text;
using System.Text;
using DPP_Compiler.Symbols;

namespace DPP_Compiler
{
    internal class Lexer
    {
        private readonly SourceText _text;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object? _value;
        
        private char Current => Peek(0);
        private char Next => Peek(1);

        public DiagnosticBag Diagnostics => _diagnostics;

        public Lexer(SourceText text)
        {
            _text = text;
        }

        public SyntaxToken Lex()
        {
            //See all possible tokens in the grammar
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
                        ConsumeStray();
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
                    else if (char.IsWhiteSpace(Current))
                    {
                        ReadWhitespace();
                    }
                    else if (char.IsLetter(Current))
                    {
                        ReadIdentifierOrKeyword();
                    }
                    else
                        ConsumeStray();
                    break;
            }

            int length = _position - _start;
            string? text = SyntaxFacts.GetText(_kind);
            if (text == null)
                text = _text.ToString(_start, length);

            return new SyntaxToken(_kind, _start, text, _value);
        }

        private void ConsumeStray()
        {
            _diagnostics.ReportStrayCharacter(_position, Current);
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
                _diagnostics.ReportInvalidNumber(new TextSpan(_start, length), text, TypeSymbol.Int);

            _value = value;
            _kind = SyntaxKind.IntegerLiteralToken;
        }

        private void ReadWhitespace()
        {
            while (char.IsWhiteSpace(Current))
                _position++;

            _kind = SyntaxKind.WhitespaceToken;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (char.IsLetter(Current))
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
                        _diagnostics.ReportUnterminatedString(span);
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
    }
}
