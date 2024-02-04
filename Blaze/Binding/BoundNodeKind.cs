namespace Blaze.Binding
{
    internal enum BoundNodeKind
    {
        //Expessions
        ErrorExpression,
        LiteralExpression,
        VariableExpression,
        AssignmentExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        ConversionExpression,

        //Statements
        ExpressionStatement,
        BlockStatement,
        GoToStatement,
        LabelStatement,
        ConditionalGotoStatement,
        VariableDeclarationStatement,
        IfStatement,
        WhileStatement,
        DoWhileStatement,
        ForStatement,
        ReturnStatement,
    }
}