﻿using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class LiteralExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken LiteralToken { get; private set; }
        public object? Value { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

        public LiteralExpressionSyntax(SyntaxToken literalToken, object? value)
        {
            LiteralToken = literalToken;
            Value = value;
        }

        public LiteralExpressionSyntax(SyntaxToken literalToken) : this(literalToken, literalToken.Value) { }
    }

}
