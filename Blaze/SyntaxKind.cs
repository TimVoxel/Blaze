﻿namespace Blaze
{
    public enum SyntaxKind
    {
        //Tokens
        IncorrectToken,
        EndOfFileToken,
        SemicolonToken,
        ColonToken,
        CommaToken,
        DotToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        OpenParenToken,
        CloseParenToken,
        OpenBraceToken,
        CloseBraceToken,
        ExclamationSignToken,
        DoubleAmpersandToken,
        DoublePipeToken,
        DoubleEqualsToken,
        DoubleDotToken,
        NotEqualsToken,
        EqualsToken,
        PlusEqualsToken,
        MinusEqualsToken,
        StarEqualsToken,
        SlashEqualsToken,

        GreaterToken,
        GreaterOrEqualsToken,
        LessToken,
        LessOrEqualsToken,
        IdentifierToken,

        //Literals
        IntegerLiteralToken,
        StringLiteralToken,

        //Keywords
        FalseKeyword,
        TrueKeyword,
        LetKeyword,
        IfKeyword,
        ElseKeyword,
        WhileKeyword,
        DoKeyword,
        ForKeyword,
        BreakKeyword,
        ContinueKeyword,
        FunctionKeyword,
        LoadKeyword,
        TickKeyword,
        ReturnKeyword,
        NamespaceKeyword,

        //Nodes
        CompilationUnit,
        TypeClause,
        ReturnTypeClause,
        ElseClause,
        GlobalStatement,
        FunctionDeclaration,
        NamespaceDeclaration,
        Parameter,

        //Expressions
        LiteralExpression,
        IdentifierExpression,
        BinaryExpression,
        ParenthesizedExpression,
        UnaryExpression,
        AssignmentExpression,
        CallExpression,

        //Statements
        BlockStatement,
        ExpressionStatement,
        VariableDeclarationStatement,
        IfStatement,
        WhileStatement,
        DoWhileStatement,
        ForStatement,
        BreakStatement,
        ContinueStatement,
        ReturnStatement,

        //Trivia 
        WhitespaceTrivia,
        LineBreakTrivia,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,
        SkippedTextTrivia,
    }
}
