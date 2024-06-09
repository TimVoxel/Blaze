namespace Blaze.Binding
{
    internal enum BoundNodeKind
    {
        //Misc
        Namespace,

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
        NopStatement,
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
        BreakStatement,
        ContinueStatement,
        ReturnStatement,
    }
}