using Blaze.Diagnostics;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;

namespace Blaze
{
    internal class Parser
    {
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;

        private int _position;
        
        private SyntaxToken Current => Peek(0);
        private SyntaxToken Next => Peek(1);
        public DiagnosticBag Diagnostics => _diagnostics;

        public Parser(SyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;

            var tokens = new List<SyntaxToken>();
            var incorrectTokens = new List<SyntaxToken>();

            var lexer = new Lexer(syntaxTree);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind == SyntaxKind.IncorrectToken)
                    incorrectTokens.Add(token); 
                else
                {
                    if (incorrectTokens.Any())
                    {
                        var leadingTrivia = token.LeadingTrivia.ToBuilder();
                        var index = 0;

                        foreach (var incorrectToken in incorrectTokens) 
                        {
                            foreach (var lt in incorrectToken.LeadingTrivia)
                                leadingTrivia.Insert(index++, lt);
                                
                            var trivia = new Trivia(syntaxTree, SyntaxKind.SkippedTextTrivia, incorrectToken.Position, incorrectToken.Text);
                            leadingTrivia.Insert(index++, trivia);

                            foreach (var tt in incorrectToken.TrailingTrivia)
                                leadingTrivia.Insert(index++, tt);    
                        }

                        incorrectTokens.Clear();
                        token = new SyntaxToken(token.Tree, token.Kind, token.Position, token.Text, token.Value, leadingTrivia.ToImmutable(), token.TrailingTrivia);
                    }
                    tokens.Add(token);
                }                    
            }
            while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToImmutableArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        private SyntaxToken TryConsume(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return Consume();

            TextLocation location = new TextLocation(_syntaxTree.Text, Current.Span);
            _diagnostics.ReportUnexpectedToken(location, Current.Kind, kind);
            return new SyntaxToken(_syntaxTree, kind, Current.Position, null, null, ImmutableArray<Trivia>.Empty, ImmutableArray<Trivia>.Empty);
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
            ImmutableArray<MemberSyntax> members = ParseMembers();
            SyntaxToken endOfFileToken = TryConsume(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(_syntaxTree, members, endOfFileToken);
        }
        
        private ImmutableArray<MemberSyntax> ParseMembers()
        {
            ImmutableArray<MemberSyntax>.Builder members = ImmutableArray.CreateBuilder<MemberSyntax>();

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                SyntaxToken startToken = Current;
                members.Add(ParseMember());

                if (Current == startToken)
                    Consume();
            }

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember()
        {
            if (Current.Kind == SyntaxKind.FunctionKeyword)
                return ParseFunctionDeclaration();
            else
                return ParseGlobalStatement();
        }

        private MemberSyntax ParseFunctionDeclaration()
        {
            SyntaxToken functionKeyword = TryConsume(SyntaxKind.FunctionKeyword);
            SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);

            SyntaxToken openParen = TryConsume(SyntaxKind.OpenParenToken);
            SeparatedSyntaxList<ParameterSyntax> parameters = ParseParameters();
            SyntaxToken closeParen = TryConsume(SyntaxKind.CloseParenToken);

            ReturnTypeClauseSyntax? returnTypeClause = null;
            if (Current.Kind == SyntaxKind.ColonToken)
                returnTypeClause = ParseReturnTypeClause();

            BlockStatementSyntax body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(_syntaxTree, functionKeyword, identifier, openParen, parameters, closeParen, returnTypeClause, body);
        }

        private MemberSyntax ParseGlobalStatement()
        {
            StatementSyntax statement = ParseStatement();
            return new GlobalStatementSyntax(_syntaxTree, statement);
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
                case SyntaxKind.DoKeyword:
                    return ParseDoWhileStatement();
                case SyntaxKind.ForKeyword:
                    return ParseForStatement();
                case SyntaxKind.BreakKeyword:
                    return ParseBreakStatement();
                case SyntaxKind.ContinueKeyword:
                    return ParseContinueStatement();
                case SyntaxKind.ReturnKeyword:
                    return ParseReturnStatement();
                case SyntaxKind.OpenParenToken:
                    return ParseExpressionStatement();
                default:
                    {
                        if (Next.Kind == SyntaxKind.IdentifierToken)
                            return ParseVariableDeclarationStatement();
                        return ParseExpressionStatement();
                    } 
            }
        }

        private StatementSyntax ParseReturnStatement()
        {
            SyntaxToken returnKeyword = TryConsume(SyntaxKind.ReturnKeyword);
            ExpressionSyntax? expression = null;
            if (Current.Kind != SyntaxKind.SemicolonToken)
                expression = ParseExpression();

            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new ReturnStatementSyntax(_syntaxTree, returnKeyword, expression, semicolon);
        }

        private StatementSyntax ParseIfStatement()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.IfKeyword);
            SyntaxToken openParen = TryConsume(SyntaxKind.OpenParenToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken closeParen = TryConsume(SyntaxKind.CloseParenToken);
            StatementSyntax body = ParseStatement();

            if (Current.Kind == SyntaxKind.ElseKeyword)
            {
                ElseClauseSyntax elseClause = ParseElseClause();
                return new IfStatementSyntax(_syntaxTree, keyword, openParen, expression, closeParen, body, elseClause);
            }
            return new IfStatementSyntax(_syntaxTree, keyword, openParen, expression, closeParen, body);
        }

        private ElseClauseSyntax ParseElseClause()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.ElseKeyword);
            StatementSyntax statement = ParseStatement();
            return new ElseClauseSyntax(_syntaxTree, keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.WhileKeyword);
            SyntaxToken openParen = TryConsume(SyntaxKind.OpenParenToken);
            ExpressionSyntax condition = ParseExpression();
            SyntaxToken closeParen = TryConsume(SyntaxKind.CloseParenToken);
            StatementSyntax body = ParseStatement();
            return new WhileStatementSyntax(_syntaxTree,  keyword, openParen, condition, closeParen, body);
        }

        private DoWhileStatementSyntax ParseDoWhileStatement()
        {
            SyntaxToken doKeyword = TryConsume(SyntaxKind.DoKeyword);
            StatementSyntax body = ParseStatement();
            SyntaxToken whileKeyword = TryConsume(SyntaxKind.WhileKeyword);
            SyntaxToken openParen = TryConsume(SyntaxKind.OpenParenToken);
            ExpressionSyntax condition = ParseExpression();
            SyntaxToken closeParen = TryConsume(SyntaxKind.CloseParenToken);
            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new DoWhileStatementSyntax(_syntaxTree, doKeyword, body, whileKeyword, openParen, condition, closeParen, semicolon);
        }

        private StatementSyntax ParseForStatement()
        {
            //For now only supports range for loops
            SyntaxToken keyword = TryConsume(SyntaxKind.ForKeyword);
            SyntaxToken openParen = TryConsume(SyntaxKind.OpenParenToken);
            SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);
            SyntaxToken equalsSign = TryConsume(SyntaxKind.EqualsToken);
            ExpressionSyntax lowerBound = ParseExpression();
            SyntaxToken doubleDot = TryConsume(SyntaxKind.DoubleDotToken);
            ExpressionSyntax upperBound = ParseExpression();
            SyntaxToken closeParen = TryConsume(SyntaxKind.CloseParenToken);

            StatementSyntax body = ParseStatement();

            return new ForStatementSyntax(_syntaxTree, keyword, openParen, identifier, equalsSign, lowerBound, doubleDot, upperBound, closeParen, body);
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
            return new BlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }
        
        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement()
        {
            SyntaxNode declarationNode;
            if (Current.Kind == SyntaxKind.IdentifierToken)
                declarationNode = ParseTypeClause();
            else
                declarationNode = TryConsume(SyntaxKind.LetKeyword);

            SyntaxToken identifierToken = TryConsume(SyntaxKind.IdentifierToken);
            SyntaxToken equalsToken = TryConsume(SyntaxKind.EqualsToken);
            ExpressionSyntax initializer = ParseExpression();
            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new VariableDeclarationStatementSyntax(_syntaxTree, declarationNode, identifierToken, equalsToken, initializer, semicolon);
        }

        private BreakStatementSyntax ParseBreakStatement()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.BreakKeyword);
            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new BreakStatementSyntax(_syntaxTree, keyword, semicolon);
        }

        private ContinueStatementSyntax ParseContinueStatement()
        {
            SyntaxToken keyword = TryConsume(SyntaxKind.ContinueKeyword);
            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new ContinueStatementSyntax(_syntaxTree, keyword, semicolon);
        }

        private TypeClauseSyntax ParseTypeClause()
        {
            SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);
            return new TypeClauseSyntax(_syntaxTree, identifier);
        }

        private ReturnTypeClauseSyntax ParseReturnTypeClause()
        {
            SyntaxToken colon = TryConsume(SyntaxKind.ColonToken);
            SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);
            return new ReturnTypeClauseSyntax(_syntaxTree, colon, identifier);
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
        {
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new ExpressionStatementSyntax(_syntaxTree, expression, semicolon);
        }

        private ExpressionSyntax ParseExpression() => ParseAssignmentExpression();

        private ExpressionSyntax ParseAssignmentExpression()
        {
            if (Current.Kind == SyntaxKind.IdentifierToken && Next.Kind == SyntaxKind.EqualsToken)
            {
                SyntaxToken identifierToken = Consume();
                SyntaxToken equalsToken = Consume();
                ExpressionSyntax expression = ParseBinaryExpression();
                return new AssignmentExpressionSyntax(_syntaxTree, identifierToken, equalsToken, expression);
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
                left = new UnaryExpressionSyntax(_syntaxTree, operatorToken, operand);
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
                left = new BinaryExpressionSyntax(_syntaxTree, left, operatorToken, right);
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
                    return ParseIdentifierOrCallExpression();
            }
        }

        private ExpressionSyntax ParseIntegerLiteral()
        {
            SyntaxToken numberToken = TryConsume(SyntaxKind.IntegerLiteralToken);
            return new LiteralExpressionSyntax(_syntaxTree, numberToken);
        }

        private ExpressionSyntax ParseBooleanLiteral()
        {
            bool isTrue = Current.Kind == SyntaxKind.TrueKeyword;
            SyntaxToken keywordToken = (isTrue) ? TryConsume(SyntaxKind.TrueKeyword) : TryConsume(SyntaxKind.FalseKeyword);
            return new LiteralExpressionSyntax(_syntaxTree, keywordToken, isTrue);
        }

        private ExpressionSyntax ParseStringLiteral()
        {
            SyntaxToken stringToken = TryConsume(SyntaxKind.StringLiteralToken);
            return new LiteralExpressionSyntax(_syntaxTree, stringToken);
        }

        private ExpressionSyntax ParseIdentifierOrCallExpression()
        {
            if (Current.Kind == SyntaxKind.IdentifierToken && Next.Kind == SyntaxKind.OpenParenToken)
                return ParseCallExpression();
            else
                return ParseIdentifierExpression();
        }

        private ExpressionSyntax ParseIdentifierExpression()
        {
            SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);
            return new IdentifierExpressionSyntax(_syntaxTree, identifier);
        }

        private ExpressionSyntax ParseCallExpression()
        {
            SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);
            SyntaxToken openParen = TryConsume(SyntaxKind.OpenParenToken);
            SeparatedSyntaxList<ExpressionSyntax> arguments = ParseArguments();
            SyntaxToken closeParen = TryConsume(SyntaxKind.CloseParenToken);
            return new CallExpressionSyntax(_syntaxTree, identifier, openParen, arguments, closeParen);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseArguments()
        {
            ImmutableArray<SyntaxNode>.Builder nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();
            bool done = false;
            while (!done && Current.Kind != SyntaxKind.CloseParenToken && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                nodesAndSeparators.Add(ParseExpression());

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    SyntaxToken comma = TryConsume(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                    done = true;
            }
            return new SeparatedSyntaxList<ExpressionSyntax>(nodesAndSeparators.ToImmutable());
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameters()
        {
            ImmutableArray<SyntaxNode>.Builder nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();
            bool done = false;
            while (!done && Current.Kind != SyntaxKind.CloseParenToken && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                TypeClauseSyntax typeClause = ParseTypeClause();
                SyntaxToken identifier = TryConsume(SyntaxKind.IdentifierToken);
                nodesAndSeparators.Add(new ParameterSyntax(_syntaxTree, typeClause, identifier));

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    SyntaxToken comma = TryConsume(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                    done = true;
            }
            return new SeparatedSyntaxList<ParameterSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
        {
            SyntaxToken left = TryConsume(SyntaxKind.OpenParenToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken right = TryConsume(SyntaxKind.CloseParenToken);
            return new ParenthesizedExpressionSyntax(_syntaxTree, left, expression, right);
        }
    }
}
