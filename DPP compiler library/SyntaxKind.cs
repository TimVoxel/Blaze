namespace DPP_Compiler
{
    public enum SyntaxKind
    {
        //Tokens
        IncorrectToken,
        EndOfFileToken,
        WhitespaceToken,
        SemicolonToken,
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
        NotEqualsToken,
        EqualsToken,
        IdentifierToken,

        //Literals
        IntegerLiteralToken,

        //Keywords
        FalseKeyword,
        TrueKeyword,
        LetKeyword,
        
        //Misc
        CompilationUnit,

        //Expressions
        LiteralExpression,
        IdentifierExpression,
        BinaryExpression,
        ParenthesizedExpression,
        UnaryExpression,
        AssignmentExpression,

        //Statements
        BlockStatement,
        ExpressionStatement,
        VariableDeclarationStatement
    }
}
