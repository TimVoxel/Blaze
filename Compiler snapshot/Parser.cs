using Compiler_snapshot.Syntax_Nodes;
using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot
{
    public class Parser
    {
        private readonly SyntaxToken[] _tokens;
        private int _position;
        private List<string> _diagnostics = new List<string>();

        private SyntaxToken Current => Peek(0);
        public IEnumerable<string> Diagnostics => _diagnostics;

        public Parser(string text)
        {
            List<SyntaxToken> tokens = new List<SyntaxToken>(); 
            Lexer lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.ConsumeNextToken();
                if (token.Kind != SyntaxKind.WhiteSpace && token.Kind != SyntaxKind.Incorrect)
                    tokens.Add(token);
            }
            while (token.Kind != SyntaxKind.EndOfFile);

            _tokens = tokens.ToArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        public SyntaxTree Parse()
        {
            ExpressionSyntax expression = ParseTerm();
            SyntaxToken endOfFileToken = Match(SyntaxKind.EndOfFile);
            return new SyntaxTree(_diagnostics, expression, endOfFileToken);
        }
        
        private ExpressionSyntax ParseTerm()
        {
            ExpressionSyntax left = ParseFactor();

            while (Current.Kind == SyntaxKind.Plus || Current.Kind == SyntaxKind.Minus)
            {
                SyntaxToken operatorToken = Consume();
                ExpressionSyntax right = ParseFactor();
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }
            return left;
        }

        private ExpressionSyntax ParseFactor()
        {
            ExpressionSyntax left = ParsePrimaryExpression();

            while (Current.Kind == SyntaxKind.Star || Current.Kind == SyntaxKind.Slash)
            {
                SyntaxToken operatorToken = Consume();
                ExpressionSyntax right = ParsePrimaryExpression();
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }
            return left;
        }
        
        private ExpressionSyntax ParseExpression()
        {
            return ParseTerm();
        }


        private ExpressionSyntax ParsePrimaryExpression()
        {
            if (Current.Kind == SyntaxKind.OpenParen)
            {
                SyntaxToken left = Consume();
                ExpressionSyntax expression = ParseExpression();
                SyntaxToken right = Match(SyntaxKind.CloseParen);
                return new ParenthesizedExpressionSyntax(left, expression, right);
            }

            SyntaxToken numberToken = Match(SyntaxKind.IntegerLiteral);
            return new NumberExpressionSyntax(numberToken);
        }

        private SyntaxToken Match(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return Consume();

            _diagnostics.Add($"ERROR: Unexpected {Current.Kind} token, expected {kind}");
            return new SyntaxToken(kind, Current.Position, null, null);
        }

        private SyntaxToken Consume()
        {
            SyntaxToken current = Current;
            _position++;
            return current;
        }

        private SyntaxToken Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _tokens.Length)
                return _tokens[_tokens.Length - 1];
            return _tokens[index];
        }
    }
}
