using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Diagnostics;
using System.Runtime.InteropServices;

namespace DPP_Compiler
{
    internal class Lexer
    {
        private readonly string _text;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object? _value;
        
        private char Current => Peek(0);
        private char Next => Peek(1);

        public DiagnosticBag Diagnostics => _diagnostics;

        public Lexer(string text)
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
                    break;
                case '|':
                    if (Next == '|')
                    {
                        _kind = SyntaxKind.DoublePipeToken;
                        _position += 2;
                    }
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
                    {
                        _diagnostics.ReportStrayCharacter(_position, Current);
                        _position++;
                    }
                    break;
            }

            int length = _position - _start;
            string? text = SyntaxFacts.GetText(_kind);
            if (text == null)
                text = _text.Substring(_start, length);

            return new SyntaxToken(_kind, _start, text, _value);
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
            string text = _text.Substring(_start, length);
            if (!int.TryParse(text, out int value))
                _diagnostics.ReportInvalidNumber(new TextSpan(_start, length), text, typeof(int));

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
            string text = _text.Substring(_start, length);
            _kind = SyntaxFacts.GetKeywordKind(text);
        }
    }
}
