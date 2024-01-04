using Compiler_snapshot.Syntax_Nodes;
using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot
{

    internal class Parser
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
                token = lexer.Lex();
                if (token.Kind != SyntaxKind.WhiteSpaceToken && token.Kind != SyntaxKind.IncorrectToken)
                    tokens.Add(token);
            }
            while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToArray();
            _diagnostics.AddRange(lexer.Diagnostics);
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

        public SyntaxTree Parse()
        {
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken endOfFileToken = Match(SyntaxKind.EndOfFileToken);
            return new SyntaxTree(_diagnostics, expression, endOfFileToken);
        }

        private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            int unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                SyntaxToken operatorToken = Consume();
                ExpressionSyntax operand = ParseExpression(unaryOperatorPrecedence);
                left = new UnaryExpressionSyntax(operatorToken, operand);
            }
            else
                left = ParsePrimaryExpression();
            
            while (true)
            {
                int precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                SyntaxToken operatorToken = Consume();
                ExpressionSyntax right = ParseExpression(precedence);
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenToken:
                {
                    SyntaxToken left = Consume();
                    ExpressionSyntax expression = ParseExpression();
                    SyntaxToken right = Match(SyntaxKind.CloseParenToken);
                    return new ParenthesizedExpressionSyntax(left, expression, right);
                }
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                {
                    SyntaxToken current = Consume();
                    bool value = current.Kind == SyntaxKind.TrueKeyword;
                    return new LiteralExpressionSyntax(current, value);
                }
                default:
                {
                    SyntaxToken numberToken = Match(SyntaxKind.IntegerLiteralToken);
                    return new LiteralExpressionSyntax(numberToken);
                }
            }
        }
    }
}
