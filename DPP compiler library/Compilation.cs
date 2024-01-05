using DPP_Compiler.Binding;
using DPP_Compiler.Diagnostics;
using DPP_Compiler.Miscellaneuos;

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

            IReadOnlyList<Diagnostic> diagnostics = Syntax.Diagnostics.Concat(binder.Diagnostics).ToArray();
            if (diagnostics.Any())
                return new EvaluationResult(diagnostics, null);

            Evaluator evaluator = new Evaluator(boundExpression, variables);
            object value = evaluator.Evaluate();
            return new EvaluationResult(Array.Empty<Diagnostic>(), value);
        }
    }
}
