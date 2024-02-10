using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.Miscellaneuos;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze
{
    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;

        public ImmutableArray<SyntaxTree> SyntaxTrees { get; private set; }
        public Compilation? Previous { get; private set; }
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    BoundGlobalScope scope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, scope, null);
                }
                return _globalScope;
            }
        }

        public Compilation(params SyntaxTree[] syntaxTrees) : this(null, syntaxTrees) { }

        private Compilation(Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            Previous = previous;
            SyntaxTrees = syntaxTrees.ToImmutableArray();
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new Compilation(this, syntaxTree);

        public IEnumerable<Symbol> GetSymbols()
        {
            Compilation? submission = this;
            HashSet<string> seenSymbolNames = new HashSet<string>(); 

            while (submission != null)
            {
                foreach (FunctionSymbol function in submission.Functions)
                    yield return function;

                foreach (VariableSymbol variable in submission.Variables)
                    if (seenSymbolNames.Add(variable.Name))
                         yield return variable;

                submission = submission.Previous;
            }
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            IEnumerable<Diagnostic> parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);
            ImmutableArray<Diagnostic> diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();

            if (diagnostics.Any())
                return new EvaluationResult(diagnostics, null);

            BoundProgram program = Binder.BindProgram(GlobalScope);

            BoundBlockStatement controlFlowGraphStatement = !program.Statement.Statements.Any() && program.Functions.Any()
                ? program.Functions.Last().Value
                : program.Statement;
            ControlFlowGraph cfg = ControlFlowGraph.Create(controlFlowGraphStatement);

            string appPath = Environment.GetCommandLineArgs()[0];
            string? appDirectory = Path.GetDirectoryName(appPath);
            if (appDirectory != null)
            {
                string cfgPath = Path.Combine(appDirectory, "cfg.dot");
                using (StreamWriter writer = new StreamWriter(cfgPath))
                    cfg.WriteTo(writer);
            }

            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);

            Evaluator evaluator = new Evaluator(program, variables);
            object? value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            BoundProgram program = Binder.BindProgram(GlobalScope);

            if (program.Statement.Statements.Any())
                program.Statement.WriteTo(writer);
            else
            {
                foreach (var functionBody in program.Functions)
                {
                    if (!GlobalScope.Functions.Contains(functionBody.Key))
                        continue;
                    functionBody.Key.WriteTo(writer);
                    functionBody.Value.WriteTo(writer);
                }
            }
        }

        public void EmitTree(FunctionSymbol function, TextWriter writer)
        {
            BoundProgram program = Binder.BindProgram(GlobalScope);

            if (!program.Functions.TryGetValue(function, out BoundBlockStatement? body))
                return;

            function.WriteTo(Console.Out);
            Console.WriteLine();
            body.WriteTo(Console.Out);
        }
    }
}
