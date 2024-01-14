using DPP_Compiler.Diagnostics;
using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Text;
using System.Collections.Immutable;

namespace DPP_Compiler
{
    internal class Parser
    {
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SourceText _text;

        private int _position;
        
        private SyntaxToken Current => Peek(0);
        private SyntaxToken Next => Peek(1);
        public DiagnosticBag Diagnostics => _diagnostics;

        public Parser(SourceText text)
        {
            _text = text;
            List<SyntaxToken> tokens = new List<SyntaxToken>(); 
            Lexer lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();
                if (token.Kind != SyntaxKind.WhitespaceToken && token.Kind != SyntaxKind.IncorrectToken)
                    tokens.Add(token);
            }
            while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToImmutableArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        private SyntaxToken TryConsume(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return Consume();

            _diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
            return new SyntaxToken(kind, _position++, null, null);
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

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            StatementSyntax expression = ParseStatement();
            SyntaxToken endOfFileToken = TryConsume(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(expression, endOfFileToken);
        }

        private StatementSyntax ParseStatement()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();
                case SyntaxKind.LetKeyword:
                    return ParseVariableDeclarationStatement();
                case SyntaxKind.IfKeyword:
                    return ParseIfStatement();
                case SyntaxKind.WhileKeyword:
                    return ParseWhileStatement();
                case SyntaxKind.ForKeyword:
                    return ParseForStatement();
                default:
                    return ParseExpressionStatement();
            }
        }

        private StatementSyntax ParseIfStatement()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.IfKeyword);
            ExpressionSyntax expression = ParseParenthesizedExpression().Expression;
            StatementSyntax statement = ParseStatement();
            if (Current.Kind == SyntaxKind.ElseKeyword)
            {
                ElseClauseSyntax elseClause = ParseElseClause();
                return new IfStatementSyntax(keyword, expression, statement, elseClause);
            }
            return new IfStatementSyntax(keyword, expression, statement);
        }

        private ElseClauseSyntax ParseElseClause()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.ElseKeyword);
            StatementSyntax statement = ParseStatement();
            return new ElseClauseSyntax(keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.WhileKeyword);
            ExpressionSyntax condition = ParseParenthesizedExpression().Expression;
            StatementSyntax body = ParseStatement();
            return new WhileStatementSyntax(keyword, condition, body);
        }

        private StatementSyntax ParseForStatement()
        {
            //For now only supports range for loops
            SyntaxToken keyword = TryConsume(SyntaxKind.ForKeyword);
            
            TryConsume(SyntaxKind.OpenParenToken);
            SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);
            SyntaxToken equalsSign = TryConsume(SyntaxKind.EqualsToken);
            ExpressionSyntax lowerBound = ParseExpression();
            SyntaxToken doubleDot = TryConsume(SyntaxKind.DoubleDotToken);
            ExpressionSyntax upperBound = ParseExpression();
            TryConsume(SyntaxKind.CloseParenToken);

            StatementSyntax body = ParseStatement();

            return new ForStatementSyntax(keyword, identifier, equalsSign, lowerBound, doubleDot, upperBound, body);
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            ImmutableArray<StatementSyntax>.Builder statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            SyntaxToken openBraceToken = TryConsume(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken && Current.Kind != SyntaxKind.CloseBraceToken)
            {
                SyntaxToken startToken = Current;
                statements.Add(ParseStatement());

                if (Current == startToken)
                    Consume();
            }

            SyntaxToken closeBraceToken = TryConsume(SyntaxKind.CloseBraceToken);
            return new BlockStatementSyntax(openBraceToken, statements.ToImmutable(), closeBraceToken);
        }
        
        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement()
        {
            SyntaxToken letKeyword = TryConsume(SyntaxKind.LetKeyword);
            SyntaxToken identifierToken = TryConsume(SyntaxKind.IdentifierToken);
            SyntaxToken equalsToken = TryConsume(SyntaxKind.EqualsToken);
            ExpressionSyntax initializer = ParseExpression();
            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new VariableDeclarationStatementSyntax(letKeyword, identifierToken, equalsToken, initializer, semicolon);
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
        {
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new ExpressionStatementSyntax(expression, semicolon);
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
                    return ParseParenthesizedExpression();
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                    return ParseBooleanLiteral();
                case SyntaxKind.IntegerLiteralToken:
                    return ParseIntegerLiteral();
                case SyntaxKind.StringLiteralToken:
                    return ParseStringLiteral();
                default:
                    return ParseIdentifierExpression();
            }
        }

        private ExpressionSyntax ParseIntegerLiteral()
        {
            SyntaxToken numberToken = TryConsume(SyntaxKind.IntegerLiteralToken);
            return new LiteralExpressionSyntax(numberToken);
        }

        private ExpressionSyntax ParseBooleanLiteral()
        {
            bool isTrue = Current.Kind == SyntaxKind.TrueKeyword;
            SyntaxToken keywordToken = (isTrue) ? TryConsume(SyntaxKind.TrueKeyword) : TryConsume(SyntaxKind.FalseKeyword);
            return new LiteralExpressionSyntax(keywordToken, isTrue);
        }

        private ExpressionSyntax ParseStringLiteral()
        {
            SyntaxToken stringToken = TryConsume(SyntaxKind.StringLiteralToken);
            return new LiteralExpressionSyntax(stringToken);
        }

        private ExpressionSyntax ParseIdentifierExpression()
        {
            SyntaxToken current = TryConsume(SyntaxKind.IdentifierToken);
            return new IdentifierExpressionSyntax(current);
        }

        private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
        {
            SyntaxToken left = TryConsume(SyntaxKind.OpenParenToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken right = TryConsume(SyntaxKind.CloseParenToken);
            return new ParenthesizedExpressionSyntax(left, expression, right);
        }
    }
}
