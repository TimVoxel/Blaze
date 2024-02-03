﻿using DPP_Compiler.Symbols;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Variable { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public override TypeSymbol Type => Variable.Type;

        public BoundVariableExpression(VariableSymbol symbol)
        {
            Variable = symbol;
        }
    }
}