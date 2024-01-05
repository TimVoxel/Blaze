using DPP_Compiler.Diagnostics;
using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler
{
    internal class Parser
    {
        private readonly SyntaxToken[] _tokens;
        private int _position;
        private DiagnosticBag _diagnostics = new DiagnosticBag();

        private SyntaxToken Current => Peek(0);
        private SyntaxToken Next => Peek(1);
        public DiagnosticBag Diagnostics => _diagnostics;

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

            _diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
            return new SyntaxToken(kind, Current.Position, "", null);
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

        private ExpressionSyntax ParseExpression() => ParseAssignmentExpression();

        private ExpressionSyntax ParseAssignmentExpression()
        {
            if (Current.Kind == SyntaxKind.IdentifierToken && Next.Kind == SyntaxKind.EqualsToken)
            {
                SyntaxToken identifierToken = Consume();
                SyntaxToken equalsToken = Consume();
                ExpressionSyntax expression = ParseBinaryExpression();
                return new AssignmentExpressionSyntax(identifierToken, equalsToken, expression);
            }

            return ParseBinaryExpression();
        }

        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            int unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                SyntaxToken operatorToken = Consume();
                ExpressionSyntax operand = ParseBinaryExpression(unaryOperatorPrecedence);
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
                ExpressionSyntax right = ParseBinaryExpression(precedence);
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
                    ExpressionSyntax expression = ParseBinaryExpression();
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
                case SyntaxKind.IdentifierToken:
                {
                    SyntaxToken current = Consume();
                    return new IdentifierExpressionSyntax(current);
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
