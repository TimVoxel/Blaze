using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot
{
    internal class Lexer
    {
        private readonly string _text;
        private int _position;
        private List<string> _diagnostics = new List<string>();

        private char Current => Peek(0);
        private char Next => Peek(1);

        public IEnumerable<string> Diagnostics => _diagnostics;
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
                    _diagnostics.Add($"The number {text} can not be represented by Int32");
                
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
                    return new SyntaxToken(SyntaxKind.PlusToken, Consume(), "+", null);
                case '-':
                    return new SyntaxToken(SyntaxKind.MinusToken, Consume(), "-", null);
                case '*':
                    return new SyntaxToken(SyntaxKind.StarToken, Consume(), "*", null);
                case '/':
                    return new SyntaxToken(SyntaxKind.SlashToken, Consume(), "/", null);
                case '(':
                    return new SyntaxToken(SyntaxKind.OpenParenToken, Consume(), "(", null);
                case ')':
                    return new SyntaxToken(SyntaxKind.CloseParenToken, Consume(), ")", null);
                case '!':
                    if (Next == '=')
                        return new SyntaxToken(SyntaxKind.NotEqualsToken, _position += 2, "!=", null);
                    return new SyntaxToken(SyntaxKind.ExclamationSignToken, Consume(), "!", null);
                case '&':
                    if (Next == '&')
                        return new SyntaxToken(SyntaxKind.DoubleAmpersandToken, _position += 2, "&&", null);
                    break;
                case '|':
                    if (Next == '|')
                        return new SyntaxToken(SyntaxKind.DoublePipeToken, _position += 2, "||", null);
                    break;
                case '=':
                    if (Next == '=')
                        return new SyntaxToken(SyntaxKind.DoubleEqualsToken, _position += 2, "==", null);
                    break;
            }
            _diagnostics.Add($"ERROR: Stray \'{Current}\' in expression");
            return new SyntaxToken(SyntaxKind.IncorrectToken, Consume(), Current.ToString(), null);
        }

        private int Consume()
        {
            _position += 1;
            return _position;
        }

        private char Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _text.Length)
                return '\0';
            return _text[index];
        } 

    }
}
