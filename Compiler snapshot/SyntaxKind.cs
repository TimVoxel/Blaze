namespace Compiler_snapshot
{
    public enum SyntaxKind
    {
        //Tokens
        IncorrectToken,
        EndOfFileToken,
        WhiteSpaceToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        OpenParenToken,
        CloseParenToken,
        ExclamationSignToken,
        DoubleAmpersandToken,
        DoublePipeToken,
        IdentifierToken,

        //Literals
        IntegerLiteralToken,

        //Keywords
        FalseKeyword,
        TrueKeyword,
        
        //Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesizedExpression,
        UnaryExpression
    }
}
