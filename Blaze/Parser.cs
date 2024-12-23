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
            if (index < 0)
                return _tokens[0];
            return _tokens[index];
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var usings = ParseUsings();
            var namespaces = ParseNamespaces();
            var endOfFileToken = TryConsume(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(_syntaxTree, usings, namespaces, endOfFileToken);
        }
        
        private ImmutableArray<UsingDirectiveSyntax> ParseUsings()
        {
            var usings = ImmutableArray.CreateBuilder<UsingDirectiveSyntax>();

            if (Current.Kind != SyntaxKind.UsingKeyword)
            {
                return ImmutableArray<UsingDirectiveSyntax>.Empty;
            }

            while (Current.Kind != SyntaxKind.NamespaceKeyword && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var startToken = Current;
                usings.Add(ParseUsing());

                if (Current == startToken)
                    Consume();
            }

            return usings.ToImmutable();
        }

        private UsingDirectiveSyntax ParseUsing()
        {
            var usingKeyword = TryConsume(SyntaxKind.UsingKeyword);
            var qualifiedName = ParseNamespaceQualifiedName();
            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new UsingDirectiveSyntax(_syntaxTree, usingKeyword, qualifiedName, semicolon);
        }

        private ImmutableArray<NamespaceDeclarationSyntax> ParseNamespaces()
        {
            var members = ImmutableArray.CreateBuilder<NamespaceDeclarationSyntax>();

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var startToken = Current;
                members.Add(ParseNamespace());

                if (Current.Kind == SyntaxKind.UsingKeyword)
                    _diagnostics.ReportUsingNotInTheBeginningOfTheFile(Current.Location);

                if (Current == startToken)
                    Consume();
            }

            return members.ToImmutable();
        }

        private NamespaceDeclarationSyntax ParseNamespace()
        {
            var namespaceKeyword = TryConsume(SyntaxKind.NamespaceKeyword);
            var identifierPath = ParseNamespaceQualifiedName();

            var openBraceToken = TryConsume(SyntaxKind.OpenBraceToken);
            var members = ParseMembers();
            var closeBraceToken = TryConsume(SyntaxKind.CloseBraceToken);

            return new NamespaceDeclarationSyntax(_syntaxTree, namespaceKeyword, identifierPath, openBraceToken, members, closeBraceToken);
        }

        private SeparatedSyntaxList<SyntaxToken> ParseNamespaceQualifiedName()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();
            var done = false;

            while (!done && Current.Kind != SyntaxKind.OpenBraceToken && Current.Kind != SyntaxKind.EndOfFileToken
                && Current.Kind != SyntaxKind.SemicolonToken)
            {
                var identifier = TryConsume(SyntaxKind.IdentifierToken);
                nodesAndSeparators.Add(identifier);

                if (Current.Kind == SyntaxKind.DotToken)
                {
                    var dot = TryConsume(SyntaxKind.DotToken);
                    nodesAndSeparators.Add(dot);
                }
                else
                    done = true;
            }
            var qualifiedName = new SeparatedSyntaxList<SyntaxToken>(nodesAndSeparators.ToImmutable());
            return qualifiedName;
        }

        private ImmutableArray<MemberSyntax> ParseMembers()
        {
            var members = ImmutableArray.CreateBuilder<MemberSyntax>();

            while (Current.Kind != SyntaxKind.CloseBraceToken && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var startToken = Current;
                members.Add(ParseMember());

                if (Current == startToken)
                    Consume();
            }

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember()
        {
            if (Current.Kind == SyntaxKind.EnumKeyword)
                return ParseEnumDeclaration();
            if (SyntaxFacts.IsFunctionModifier(Current.Kind) || Current.Kind == SyntaxKind.FunctionKeyword)
                return ParseFunctionDeclaration();
            else if (Current.Kind == SyntaxKind.VarKeyword || Current.Kind == SyntaxKind.IdentifierToken)
                return ParseFieldDeclaration();
            else
                return ParseGlobalStatement();
        }

        private MemberSyntax ParseFunctionDeclaration()
        {
            var modifiers = ImmutableArray.CreateBuilder<SyntaxToken>();
            var modifierKinds = new HashSet<SyntaxKind>();

            while (SyntaxFacts.IsFunctionModifier(Current.Kind))
            {
                var current = Consume();
                if (modifierKinds.Contains(current.Kind))
                {
                    var location = new TextLocation(_syntaxTree.Text, current.Span);
                    _diagnostics.ReportDuplicateFunctionModifier(location);
                }

                modifiers.Add(current);
                modifierKinds.Add(current.Kind);
            }

            var functionKeyword = TryConsume(SyntaxKind.FunctionKeyword);
            var identifier = TryConsume(SyntaxKind.IdentifierToken);

            var openParen = TryConsume(SyntaxKind.OpenParenToken);
            var parameters = ParseParameters();
            var closeParen = TryConsume(SyntaxKind.CloseParenToken);
            
            ReturnTypeClauseSyntax? returnTypeClause = null;
            if (Current.Kind == SyntaxKind.ColonToken)
                returnTypeClause = ParseReturnTypeClause();

            var body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(_syntaxTree, modifiers.ToImmutable(), functionKeyword, identifier, openParen, parameters, closeParen, returnTypeClause, body);
        }

        private DeclarationSyntax ParseFieldDeclaration()
        {
            SyntaxNode typeNode;
            if (Current.Kind == SyntaxKind.IdentifierToken)
                typeNode = ParseTypeClause();
            else
                typeNode = TryConsume(SyntaxKind.VarKeyword);

            var identifierToken = TryConsume(SyntaxKind.IdentifierToken);
            var equalsToken = TryConsume(SyntaxKind.EqualsToken);
            var initializer = ParseExpression();
            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new FieldDeclarationSyntax(_syntaxTree, typeNode, identifierToken, equalsToken, initializer, semicolon);
        }

        private DeclarationSyntax ParseEnumDeclaration()
        {
            var enumKeyword = TryConsume(SyntaxKind.EnumKeyword);
            var identifier = TryConsume(SyntaxKind.IdentifierToken);
            var openBrace = TryConsume(SyntaxKind.OpenBraceToken);

            var memberDeclarations = ImmutableArray.CreateBuilder<EnumMemberDeclarationSyntax>();

            while (Current.Kind != SyntaxKind.CloseBraceToken && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var startToken = Current;
                memberDeclarations.Add(ParseEnumMemberDeclaration());

                if (Current == startToken)
                    Consume();
            }
            
            var closeBrace = TryConsume(SyntaxKind.CloseBraceToken);

            return new EnumDeclarationSyntax(_syntaxTree, enumKeyword, identifier, openBrace, memberDeclarations.ToImmutable(), closeBrace);
        }

        private EnumMemberDeclarationSyntax ParseEnumMemberDeclaration()
        {
            var mustHaveComma = Next.Kind == SyntaxKind.IdentifierToken || Next.Kind == SyntaxKind.CommaToken;
            var identifier = TryConsume(SyntaxKind.IdentifierToken);

            EnumMemberEqualsSyntax? equalsSyntax = null;
            if (Current.Kind == SyntaxKind.EqualsToken)
            {
                var equalsToken = TryConsume(SyntaxKind.EqualsToken);
                var literalToken = TryConsume(SyntaxKind.IntegerLiteralToken);
                equalsSyntax = new EnumMemberEqualsSyntax(_syntaxTree, equalsToken, literalToken);
            }

            SyntaxToken? commaToken = null;
            if (mustHaveComma)
                commaToken = TryConsume(SyntaxKind.CommaToken);

            return new EnumMemberDeclarationSyntax(_syntaxTree, identifier, equalsSyntax, commaToken);
        }

        private MemberSyntax ParseGlobalStatement()
        {
            StatementSyntax statement = ParseStatement();
            _diagnostics.ReportGlobalStatement(statement.Location);
            return new GlobalStatementSyntax(_syntaxTree, statement);
        }

        private StatementSyntax ParseStatement()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();
                case SyntaxKind.VarKeyword:
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
                        ExpressionSyntax expression = ParseExpression();

                        if (Current.Kind == SyntaxKind.SemicolonToken)
                        {
                            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
                            return new ExpressionStatementSyntax(_syntaxTree, expression, semicolon);
                        }
                        else
                            return ParseVariableDeclarationStatement(expression);
                    } 
            }
        }

        private StatementSyntax ParseReturnStatement()
        {
            var returnKeyword = TryConsume(SyntaxKind.ReturnKeyword);
            ExpressionSyntax? expression = null;
            if (Current.Kind != SyntaxKind.SemicolonToken)
                expression = ParseExpression();

            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new ReturnStatementSyntax(_syntaxTree, returnKeyword, expression, semicolon);
        }

        private StatementSyntax ParseIfStatement()
        {
            var keyword = TryConsume(SyntaxKind.IfKeyword);
            var openParen = TryConsume(SyntaxKind.OpenParenToken);
            var expression = ParseExpression();
            var closeParen = TryConsume(SyntaxKind.CloseParenToken);
            var body = ParseStatement();

            if (Current.Kind == SyntaxKind.ElseKeyword)
            {
                var elseClause = ParseElseClause();
                return new IfStatementSyntax(_syntaxTree, keyword, openParen, expression, closeParen, body, elseClause);
            }
            return new IfStatementSyntax(_syntaxTree, keyword, openParen, expression, closeParen, body);
        }

        private ElseClauseSyntax ParseElseClause()
        {
            var keyword = TryConsume(SyntaxKind.ElseKeyword);
            var statement = ParseStatement();
            return new ElseClauseSyntax(_syntaxTree, keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            var keyword = TryConsume(SyntaxKind.WhileKeyword);
            var openParen = TryConsume(SyntaxKind.OpenParenToken);
            var condition = ParseExpression();
            var closeParen = TryConsume(SyntaxKind.CloseParenToken);
            var body = ParseStatement();
            return new WhileStatementSyntax(_syntaxTree,  keyword, openParen, condition, closeParen, body);
        }

        private DoWhileStatementSyntax ParseDoWhileStatement()
        {
            var doKeyword = TryConsume(SyntaxKind.DoKeyword);
            var body = ParseStatement();
            var whileKeyword = TryConsume(SyntaxKind.WhileKeyword);
            var openParen = TryConsume(SyntaxKind.OpenParenToken);
            var condition = ParseExpression();
            var closeParen = TryConsume(SyntaxKind.CloseParenToken);
            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new DoWhileStatementSyntax(_syntaxTree, doKeyword, body, whileKeyword, openParen, condition, closeParen, semicolon);
        }

        private StatementSyntax ParseForStatement()
        {
            //For now only supports range for loops
            var keyword = TryConsume(SyntaxKind.ForKeyword);
            var openParen = TryConsume(SyntaxKind.OpenParenToken);
            var identifier = TryConsume(SyntaxKind.IdentifierToken);
            var equalsSign = TryConsume(SyntaxKind.EqualsToken);
            var lowerBound = ParseExpression();
            var doubleDot = TryConsume(SyntaxKind.DoubleDotToken);
            var upperBound = ParseExpression();
            var closeParen = TryConsume(SyntaxKind.CloseParenToken);
            var body = ParseStatement();

            return new ForStatementSyntax(_syntaxTree, keyword, openParen, identifier, equalsSign, lowerBound, doubleDot, upperBound, closeParen, body);
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
            var openBraceToken = TryConsume(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken && Current.Kind != SyntaxKind.CloseBraceToken)
            {
                var startToken = Current;
                statements.Add(ParseStatement());

                if (Current == startToken)
                   Consume();
            }

            var closeBraceToken = TryConsume(SyntaxKind.CloseBraceToken);
            return new BlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }
        
        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement(ExpressionSyntax? typeExpression = null)
        {
            SyntaxNode declarationNode;
            
            if (typeExpression != null)
                declarationNode = new TypeClauseSyntax(_syntaxTree, typeExpression);
            else
                declarationNode = TryConsume(SyntaxKind.VarKeyword);

            var identifierToken = TryConsume(SyntaxKind.IdentifierToken);
            var equalsToken = TryConsume(SyntaxKind.EqualsToken);
            var initializer = ParseExpression();
            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new VariableDeclarationStatementSyntax(_syntaxTree, declarationNode, identifierToken, equalsToken, initializer, semicolon);
        }

        private BreakStatementSyntax ParseBreakStatement()
        {
            var keyword = TryConsume(SyntaxKind.BreakKeyword);
            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new BreakStatementSyntax(_syntaxTree, keyword, semicolon);
        }

        private ContinueStatementSyntax ParseContinueStatement()
        {
            var keyword = TryConsume(SyntaxKind.ContinueKeyword);
            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new ContinueStatementSyntax(_syntaxTree, keyword, semicolon);
        }

        private TypeClauseSyntax ParseTypeClause()
        {
            var identifier = ParsePrimaryExpression();
            return new TypeClauseSyntax(_syntaxTree, identifier);
        }

        private ReturnTypeClauseSyntax ParseReturnTypeClause()
        {
            var colon = TryConsume(SyntaxKind.ColonToken);
            var identifier = ParsePrimaryExpression();
            return new ReturnTypeClauseSyntax(_syntaxTree, colon, identifier);
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
        {
            var expression = ParseExpression();
            var semicolon = TryConsume(SyntaxKind.SemicolonToken);
            return new ExpressionStatementSyntax(_syntaxTree, expression, semicolon);
        }

        private ExpressionSyntax ParseExpression()
        {
            //This can be used outside of an expression statement context
            //So have to add an extra check

            if (Current.Kind.GetUnaryOperatorPrecedence() != 0)
                return ParseBinaryOrUnaryExpression();

            //Statement context
            var left = ParsePrimaryExpression();

            if (Current.Kind == SyntaxKind.DoubleMinusToken || Current.Kind == SyntaxKind.DoublePlusToken)
                return ParseIncrementOrDecrement(left);
            else if (Current.Kind.GetAssignmentOperatorPrecedence() != 0)
                return ParseAssignmentExpression(left);
            else
                return ParseBinaryOrUnaryExpression(0, left);
        }

        private ExpressionSyntax ParseAssignmentExpression(ExpressionSyntax left)
        {
            var assignmentOperator = Consume();
            var right = ParseExpression();
            return new AssignmentExpressionSyntax(_syntaxTree, left, assignmentOperator, right);
        }

        private ExpressionSyntax ParseIncrementOrDecrement(ExpressionSyntax operand)
        {
            var op = Consume();
            return new IncrementExpressionSyntax(_syntaxTree, operand, op);
        }

        private ExpressionSyntax ParseBinaryOrUnaryExpression(int parentPrecedence = 0, ExpressionSyntax? left = null)
        {
            //Left is only not null when we are passing it after parsing it in ParseExpression
            //In this case unary expressions cannot occur

            if (left == null)
            {
                var unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();

                if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
                {
                    var operatorToken = Consume();
                    var operand = ParseBinaryOrUnaryExpression(unaryOperatorPrecedence);
                    left = new UnaryExpressionSyntax(_syntaxTree, operatorToken, operand);
                }
                else
                    left = ParsePrimaryExpression();
            }

            while (true)
            {
                var binaryPrecedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (binaryPrecedence == 0 || binaryPrecedence <= parentPrecedence)
                    break;

                var operatorToken = Consume();
                var right = ParseBinaryOrUnaryExpression(binaryPrecedence);
                left = new BinaryExpressionSyntax(_syntaxTree, left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            //TODO: add separate more intuitive diagnostics for invalid member access
        
            ExpressionSyntax expression;

            switch (Current.Kind)
            {
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                    expression = ParseBooleanLiteral();
                    break;
                case SyntaxKind.IntegerLiteralToken:
                    expression = ParseIntegerLiteral();
                    break;
                case SyntaxKind.FloatLiteralToken:
                    expression = ParseFloatLiteral();
                    break;
                case SyntaxKind.DoubleLiteralToken:
                    expression = ParseDoubleLiteral();
                    break;
                case SyntaxKind.StringLiteralToken:
                    expression = ParseStringLiteral();
                    break;
                case SyntaxKind.NewKeyword:
                    expression = ParseObjectOrArrayCreationExpression();
                    break;
                default:
                    ExpressionSyntax previous;
                    if (Current.Kind == SyntaxKind.OpenParenToken)
                        previous = ParseParenthesizedExpression();
                    else 
                        previous = ParseSimpleNameExpression();

                    while (true)
                    {
                        if (Current.Kind == SyntaxKind.OpenParenToken)
                            previous = ParseCallExpression(previous);
                        else if (Current.Kind == SyntaxKind.OpenSquareBracketToken)
                        {
                            if (Next.Kind == SyntaxKind.CommaToken || Next.Kind == SyntaxKind.CloseSquareBracketToken)
                                previous = ParseArrayTypeExpression(previous);
                            else
                                previous = ParseArrayAccessExpression(previous);
                        }
                        else if (Current.Kind == SyntaxKind.DotToken)
                        {
                            previous = ParseMemberAccessExpression(previous);   
                        }
                        //else if (previous.Kind == SyntaxKind.MemberAccessExpression &&
                        //    Current.Kind == SyntaxKind.IdentifierToken)
                        //{
                        //    _diagnostics.ReportInfo(Current.Location, $"Whatever this is");
                        ///    previous = ParseSimpleNameExpression();
                        //    break;
                        //}
                        else
                            break;
                    }
                    expression = previous;
                    break;
            }
            return expression;
        }

        private ExpressionSyntax ParseArrayTypeExpression(ExpressionSyntax identifier)
        {
            var openSquareBracket = TryConsume(SyntaxKind.OpenSquareBracketToken);
            var rankBuilder = ImmutableArray.CreateBuilder<SyntaxToken>();
            
            while (Current.Kind == SyntaxKind.CommaToken)
                rankBuilder.Add(TryConsume(SyntaxKind.CommaToken));

            var closeSquareBracket = TryConsume(SyntaxKind.CloseSquareBracketToken);
            return new ArrayTypeExpressionSyntax(_syntaxTree, identifier, openSquareBracket, rankBuilder.ToImmutable(), closeSquareBracket); 
        }

        private ExpressionSyntax ParseArrayAccessExpression(ExpressionSyntax identifier)
        {
            var openSquareBracket = TryConsume(SyntaxKind.OpenSquareBracketToken);
            var arguments = ParseArguments(SyntaxKind.CloseSquareBracketToken);
            var closeSquareBracket = TryConsume(SyntaxKind.CloseSquareBracketToken);
            return new ArrayAccessExpressionSyntax(_syntaxTree, identifier, openSquareBracket, arguments, closeSquareBracket);
        }

        private ExpressionSyntax ParseSimpleNameExpression()
        {
            var identifier = TryConsume(SyntaxKind.IdentifierToken);
            return new SimpleNameExpressionSyntax(_syntaxTree, identifier);
        }

        private ExpressionSyntax ParseCallExpression(ExpressionSyntax identifier)
        {
            var openParen = TryConsume(SyntaxKind.OpenParenToken);
            var arguments = ParseArguments(SyntaxKind.CloseParenToken);
            var closeParen = TryConsume(SyntaxKind.CloseParenToken);
            return new CallExpressionSyntax(_syntaxTree, identifier, openParen, arguments, closeParen);
        }
        
        private ExpressionSyntax ParseMemberAccessExpression(ExpressionSyntax left)
        {   
            var dotToken = TryConsume(SyntaxKind.DotToken);
            var memberIdentifier = TryConsume(SyntaxKind.IdentifierToken);
            return new MemberAccessExpressionSyntax(_syntaxTree, left, dotToken, memberIdentifier);
        }

        private ExpressionSyntax ParseObjectOrArrayCreationExpression()
        {
            var keyword = TryConsume(SyntaxKind.NewKeyword);

            ExpressionSyntax identifier = ParseSimpleNameExpression();

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (Current.Kind == SyntaxKind.DotToken)
                    identifier = ParseMemberAccessExpression(identifier);
                else if (Current.Kind == SyntaxKind.IdentifierToken)
                {
                    identifier = ParseSimpleNameExpression();
                    break;
                }
                else
                    break;
            }

            if (Current.Kind == SyntaxKind.OpenSquareBracketToken)
            {
                return ParseArrayCreationExpression(keyword, identifier);
            }
            else 
            {
                var openParen = TryConsume(SyntaxKind.OpenParenToken);
                var arguments = ParseArguments(SyntaxKind.CloseParenToken);
                var closeParen = TryConsume(SyntaxKind.CloseParenToken);
                return new ObjectCreationExpressionSyntax(_syntaxTree, keyword, identifier, openParen, arguments, closeParen);
            }
        }

        private ExpressionSyntax ParseArrayCreationExpression(SyntaxToken keyword, ExpressionSyntax identifier)
        {
            var openSquareBracket = TryConsume(SyntaxKind.OpenSquareBracketToken);
            var arguments = ImmutableArray.CreateBuilder<SyntaxNode>();
            var done = false;

            if (Current.Kind == SyntaxKind.CommaToken)
            {
                while (Current.Kind == SyntaxKind.CommaToken)
                    arguments.Add(TryConsume(SyntaxKind.CommaToken));
            }
            else
            {
                while (!done && Current.Kind != SyntaxKind.CloseSquareBracketToken && Current.Kind != SyntaxKind.EndOfFileToken)
                {
                    arguments.Add(ParseExpression());

                    if (Current.Kind == SyntaxKind.CommaToken)
                    {
                        var comma = TryConsume(SyntaxKind.CommaToken);
                        arguments.Add(comma);
                    }
                    else
                        done = true;
                }
            }
            
            var closeSquareBracket = TryConsume(SyntaxKind.CloseSquareBracketToken);
            return new ArrayCreationExpressionSyntax(_syntaxTree, keyword, identifier, openSquareBracket, arguments.ToImmutable(), closeSquareBracket);
        }

        private ExpressionSyntax ParseIntegerLiteral() => ParseLiteral(SyntaxKind.IntegerLiteralToken);
        private ExpressionSyntax ParseFloatLiteral() => ParseLiteral(SyntaxKind.FloatLiteralToken);
        private ExpressionSyntax ParseDoubleLiteral() => ParseLiteral(SyntaxKind.DoubleLiteralToken);
        private ExpressionSyntax ParseBooleanLiteral() => ParseLiteral(Current.Kind, Current.Kind == SyntaxKind.TrueKeyword);
        private ExpressionSyntax ParseStringLiteral() => ParseLiteral(SyntaxKind.StringLiteralToken);
        
        private ExpressionSyntax ParseLiteral(SyntaxKind kind, object? value = null)
        {
            var token = TryConsume(kind);
            return new LiteralExpressionSyntax(_syntaxTree, token, value ?? token.Value);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseArguments(SyntaxKind closing)
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();
            var done = false;

            while (!done && Current.Kind != closing && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                nodesAndSeparators.Add(ParseExpression());

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = TryConsume(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                    done = true;
            }
            return new SeparatedSyntaxList<ExpressionSyntax>(nodesAndSeparators.ToImmutable());
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameters()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();
            var done = false;
            while (!done && Current.Kind != SyntaxKind.CloseParenToken && Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var typeClause = ParseTypeClause();
                var identifier = TryConsume(SyntaxKind.IdentifierToken);
                nodesAndSeparators.Add(new ParameterSyntax(_syntaxTree, typeClause, identifier));

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = TryConsume(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                    done = true;
            }
            return new SeparatedSyntaxList<ParameterSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
        {
            var openParen = TryConsume(SyntaxKind.OpenParenToken);
            var expression = ParseExpression();
            var closeParen = TryConsume(SyntaxKind.CloseParenToken);
            
            return new ParenthesizedExpressionSyntax(_syntaxTree, openParen, expression, closeParen);
        }
    }
}
