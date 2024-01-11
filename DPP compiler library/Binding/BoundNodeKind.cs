namespace DPP_Compiler.Binding
{
    internal enum BoundNodeKind
    {
        //Expessions
        LiteralExpression,
        VariableExpression,
        AssignmentExpression,
        UnaryExpression,
        BinaryExpression,

        //Statements
        ExpressionStatement,
        BlockStatement,
    }
}