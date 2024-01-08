using DPP_Compiler.Binding;
using DPP_Compiler.Diagnostics;
using DPP_Compiler.Miscellaneuos;
using System.Collections.Immutable;

namespace DPP_Compiler
{
    public sealed class Compilation
    {
        public SyntaxTree Syntax { get; private set; }

        public Compilation(SyntaxTree syntax)
        {
            Syntax = syntax;
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            Binder binder = new Binder(variables);
            BoundExpression boundExpression = binder.BindExpression(Syntax.Root);

            ImmutableArray<Diagnostic> diagnostics = Syntax.Diagnostics.Concat(binder.Diagnostics).ToImmutableArray();
            if (diagnostics.Any())
                return new EvaluationResult(diagnostics, null);

            Evaluator evaluator = new Evaluator(boundExpression, variables);
            object value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }
    }
}
