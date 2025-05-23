﻿using Blaze.SyntaxTokens;
using Blaze.Diagnostics;
using Blaze.Text;
using System.Text;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze
{
    internal class Lexer : IDiagnosticsSource
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly DiagnosticBag _diagnostics;

        private ImmutableArray<Trivia>.Builder _triviaBuilder = ImmutableArray.CreateBuilder<Trivia>();
        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object? _value;
        public string DiagnosticsSourceName => "Lexer";
        
        private char Current => Peek(0);
        private char Next => Peek(1);

        public DiagnosticBag Diagnostics => _diagnostics;

        public Lexer(SyntaxTree syntaxTree)
        {
            _text = syntaxTree.Text;
            _syntaxTree = syntaxTree;
            _diagnostics = new DiagnosticBag(this);
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
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.PlusEqualsToken;
                        _position += 2;
                    }
                    else if (Next == '+')
                    {
                        _kind = SyntaxKind.DoublePlusToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.PlusToken);
                    break;
                case '-':
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.MinusEqualsToken;
                        _position += 2;
                    }
                    else if (Next == '-')
                    {
                        _kind = SyntaxKind.DoubleMinusToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.MinusToken);
                    break;
                case '*':
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.StarEqualsToken;
                        _position += 2;
                    }
                    else
                        ConsumeOfKind(SyntaxKind.StarToken);
                    break;
                case '/':
                    if (Next == '=')
                    {
                        _kind = SyntaxKind.SlashEqualsToken;
                        _position += 2;
                    }
                    else
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
                case '[':
                    ConsumeOfKind(SyntaxKind.OpenSquareBracketToken);
                    break;
                case ']':
                    ConsumeOfKind(SyntaxKind.CloseSquareBracketToken);
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
                        ReadNumberLiteral();
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
            _kind = SyntaxKind.IncorrectToken;
            _position++;
        }

        private void ConsumeOfKind(SyntaxKind kind)
        {
            _kind = kind;
            _position++;
        }

        private char Peek(int offset)
        {
            var index = _position + offset;
            if (index >= _text.Length)
                return '\0';
            return _text[index];
        }

        private void ReadNumberLiteral()
        {
            while (char.IsDigit(Current))
                _position++;

            if ((Current == '.' && Next != '.') || Current == 'f' || Current == 'F')
            {   
                ReadFloatOrDoubleLiteral();
            } 
            else
                ReadIntegerLiteral();
        }

        private void ReadIntegerLiteral()
        {
            var length = _position - _start;
            var text = _text.ToString(_start, length);

            if (!int.TryParse(text, out int value))
                ReportInvalidNumber(text, TypeSymbol.Int);
                
            _value = value;
            _kind = SyntaxKind.IntegerLiteralToken;
        }

        private void ReadFloatOrDoubleLiteral()
        {

            if (Current == '.')
                _position++;

            while (char.IsDigit(Current))
                _position++;

            if (Current == 'f' || Current == 'F')
            {
                _position++;
                var length = _position - _start - 1;
                var text = _text.ToString(_start, length);

                if (!float.TryParse(text, out float value))
                    ReportInvalidNumber(text, TypeSymbol.Float);

                _value = value;
                _kind = SyntaxKind.FloatLiteralToken;
            }
            else
            {
                var length = _position - _start;
                var text = _text.ToString(_start, length);

                if (!double.TryParse(text, out double value))
                    ReportInvalidNumber(text, TypeSymbol.Double);
                
                _value = value;
                _kind = SyntaxKind.DoubleLiteralToken;
            }    
        }

        private void ReportInvalidNumber(string text, TypeSymbol type)
        {
            var span = new TextSpan(_start, text.Length);
            var location = new TextLocation(_text, span);
            _diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Float);
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
            var length = _position - _start;
            var text = _text.ToString(_start, length);
            _kind = SyntaxFacts.GetKeywordKind(text);
        }

        private void ReadStringLiteral()
        {
            _position++;
            var value = new StringBuilder();

            var done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        var span = new TextSpan(_start, 1);
                        var location = new TextLocation(_text, span);
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
            var done = false;
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
            var done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                        var span = new TextSpan(_start, 2);
                        var location = new TextLocation(_text, span);
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
