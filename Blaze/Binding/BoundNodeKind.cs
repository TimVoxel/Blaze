﻿namespace Blaze.Binding
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
        CompoundAssignmentExpression,
        IncrementExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        MethodCallExpression,
        ConversionExpression,
        ObjectCreationExpression,
        ArrayCreationExpression,
        TypeExpression,
        EnumExpression,
        NamespaceExpression,
        FunctionExpression,
        FieldAccessExpression,
        MethodAccessExpression,
        ArrayAccessExpression,
        ThisExpression,

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