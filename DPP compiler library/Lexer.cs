using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Diagnostics;

namespace DPP_Compiler
{
    internal class Lexer
    {
        private readonly string _text;
        private int _position;
        private DiagnosticBag _diagnostics = new DiagnosticBag();

        private char Current => Peek(0);
        private char Next => Peek(1);

        public DiagnosticBag Diagnostics => _diagnostics;
        private bool IsOutOfBounds => _position >= _text.Length;

        public Lexer(string text)
        {
            _text = text;
        }

        public SyntaxToken Lex()
        {
            //See all possible tokens in the grammar

            if (IsOutOfBounds) return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);

            if (char.IsDigit(Current))
            {
                int start = _position;
                while (char.IsDigit(Current))
                    Consume();

                int length = _position - start;
                string text = _text.Substring(start, length);
                if (!int.TryParse(text, out int value))
                    _diagnostics.ReportInvalidNumber(new TextSpan(start, length), text, typeof(int));
                
                return new SyntaxToken(SyntaxKind.IntegerLiteralToken, start, text, value);
            }
            if (char.IsWhiteSpace(Current))
            {
                int start = _position;
                while (char.IsWhiteSpace(Current))
                    Consume();
                int length = _position - start;
                string text = _text.Substring(start, length);
                return new SyntaxToken(SyntaxKind.WhiteSpaceToken, start, text, null);
            }

            if (char.IsLetter(Current))
            {
                int start = _position;
                while (char.IsLetter(Current))
                    Consume();
                int length = _position - start;
                string text = _text.Substring(start, length);
                SyntaxKind keywordKind = SyntaxFacts.GetKeywordKind(text);
                return new SyntaxToken(keywordKind, start, text, null);
            }

            switch (Current)
            {
                case '+':
                    return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
                case '-':
                    return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
                case '*':
                    return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
                case '/':
                    return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
                case '(':
                    return new SyntaxToken(SyntaxKind.OpenParenToken, _position++, "(", null);
                case ')':
                    return new SyntaxToken(SyntaxKind.CloseParenToken, _position++, ")", null);
                case '!':
                    if (Next == '=') 
                        return Consume(SyntaxKind.NotEqualsToken, "!=");
                    return new SyntaxToken(SyntaxKind.ExclamationSignToken, _position++, "!", null);
                case '&':
                    if (Next == '&') 
                        return Consume(SyntaxKind.DoubleAmpersandToken, "&&");
                    break;
                case '|':
                    if (Next == '|')
                        return Consume(SyntaxKind.DoublePipeToken, "||");
                    break;
                case '=':
                    if (Next == '=') 
                        return Consume(SyntaxKind.DoubleEqualsToken, "==");
                    return new SyntaxToken(SyntaxKind.EqualsToken, _position++, "=", null);
            }
            _diagnostics.ReportStrayCharacter(_position, Current);
            return new SyntaxToken(SyntaxKind.IncorrectToken, _position++, Current.ToString(), null);
        }

        private void Consume() => _position++;

        private SyntaxToken Consume(SyntaxKind kind, string tokenText, object? value)
        {
            SyntaxToken token = new SyntaxToken(kind, _position, tokenText, value);
            _position += tokenText.Length;
            return token;
        }

        private SyntaxToken Consume(SyntaxKind kind, string tokenText) => Consume(kind, tokenText, null);

        private char Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _text.Length)
                return '\0';
            return _text[index];
        } 

    }
}
