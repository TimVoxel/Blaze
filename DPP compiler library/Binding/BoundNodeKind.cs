namespace DPP_Compiler.Binding
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

        //Statements
        ExpressionStatement,
        BlockStatement,
        GoToStatement,
        LabelStatement,
        ConditionalGotoStatement,
        VariableDeclarationStatement,
        IfStatement,
        WhileStatement,
        ForStatement,
    }
}