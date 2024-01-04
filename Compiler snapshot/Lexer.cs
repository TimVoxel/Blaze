using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot
{
    public class Lexer
    {
        private readonly string _text;
        private int _position;
        private List<string> _diagnostics = new List<string>();

        private char Current
        {
            get
            {
                if (IsOutOfBounds) return '\0';
                return _text[_position];
            }
        }

        public IEnumerable<string> Diagnostics => _diagnostics;

        private bool IsOutOfBounds => _position >= _text.Length;

        public Lexer(string text)
        {
            _text = text;
        }

        public SyntaxToken ConsumeNextToken()
        {
            //See all possible tokens in the grammar

            if (IsOutOfBounds) return new SyntaxToken(SyntaxKind.EndOfFile, _position, "\0", null);

            if (char.IsDigit(Current))
            {
                int start = _position;
                while (char.IsDigit(Current))
                    Consume();

                int length = _position - start;
                string text = _text.Substring(start, length);
                if (!int.TryParse(text, out int value))
                    _diagnostics.Add($"The number {text} can not be represented by Int32");
                
                return new SyntaxToken(SyntaxKind.IntegerLiteral, start, text, value);
            }
            if (char.IsWhiteSpace(Current))
            {
                int start = _position;
                while (char.IsWhiteSpace(Current))
                    Consume();
                int length = _position - start;
                string text = _text.Substring(start, length);
                return new SyntaxToken(SyntaxKind.WhiteSpace, start, text, null);
            }

            if (Current == '+')
                return new SyntaxToken(SyntaxKind.Plus, Consume(), "+", null);
            if (Current == '-')
                return new SyntaxToken(SyntaxKind.Minus, Consume(), "-", null);
            if (Current == '*')
                return new SyntaxToken(SyntaxKind.Star, Consume(), "*", null);
            if (Current == '/')
                return new SyntaxToken(SyntaxKind.Slash, Consume(), "/", null);
            if (Current == '(')
                return new SyntaxToken(SyntaxKind.OpenParen, Consume(), "(", null);
            if (Current == ')')
                return new SyntaxToken(SyntaxKind.CloseParen, Consume(), ")", null);

            _diagnostics.Add($"ERROR: Stray \'{Current}\' in expression");
            return new SyntaxToken(SyntaxKind.Incorrect, Consume(), Current.ToString(), null);
        }

        private int Consume()
        {
            _position += 1;
            return _position;
        }

    }
}
