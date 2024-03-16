using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.Emit;
using Blaze.Miscellaneuos;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze
{
    public sealed class Compilation
    {
        private BoundGlobalScope? _globalScope;
        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope == null)
                {
                    BoundGlobalScope scope = Binder.BindGlobalScope(SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, scope, null);
                }
                return _globalScope;
            }
        }

        public ImmutableArray<SyntaxTree> SyntaxTrees { get; private set; }
        public ImmutableArray<FunctionSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VariableSymbol> Variables => GlobalScope.Variables;
        public FunctionSymbol? MainFunction => GlobalScope.MainFunction;

        private Compilation(params SyntaxTree[] trees)
        {
            SyntaxTrees = trees.ToImmutableArray();
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees) => new Compilation(syntaxTrees);

        public IEnumerable<Symbol> GetSymbols()
        {
            Compilation submission = this;
            HashSet<string> seenSymbolNames = new HashSet<string>(); 

            while (submission != null)
            {
                foreach (FunctionSymbol function in submission.Functions)
                    yield return function;

                foreach (VariableSymbol variable in submission.Variables)
                    if (seenSymbolNames.Add(variable.Name))
                         yield return variable;
            }
        }

        private BoundProgram GetProgram() => Binder.BindProgram(GlobalScope);
 
        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            IEnumerable<Diagnostic> parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);
            ImmutableArray<Diagnostic> diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();

            if (diagnostics.Any())
                return new EvaluationResult(diagnostics, null);

            BoundProgram program = GetProgram();

            //EmitControlFlowGraph(program);

            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);

            Evaluator evaluator = new Evaluator(program, variables);
            object? value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.MainFunction != null)
                EmitTree(GlobalScope.MainFunction, writer);
        }

        public void EmitTree(FunctionSymbol function, TextWriter writer)
        {
            BoundProgram program = GetProgram();

            if (!program.Functions.TryGetValue(function, out BoundBlockStatement? body))
                return;

            function.WriteTo(Console.Out);
            Console.WriteLine();
            body.WriteTo(Console.Out);
        }

        public ImmutableArray<Diagnostic> Emit(string moduleName, string[] references, string outputPath)
        {
            BoundProgram program = GetProgram();
            return ILEmitter.Emit(program, moduleName, references, outputPath);
        }

        private void EmitControlFlowGraph(BoundProgram program)
        {
            BoundBlockStatement? controlFlowGraphStatement = program.Functions.Last().Value.Statements.Any() && program.Functions.Any()
                ? program.Functions.Last().Value
                : null;

            if (controlFlowGraphStatement == null)
                return;

            ControlFlowGraph cfg = ControlFlowGraph.Create(controlFlowGraphStatement);

            string appPath = Environment.GetCommandLineArgs()[0];
            string? appDirectory = Path.GetDirectoryName(appPath);
            if (appDirectory != null)
            {
                string cfgPath = Path.Combine(appDirectory, "cfg.dot");
                using (StreamWriter writer = new StreamWriter(cfgPath))
                    cfg.WriteTo(writer);
            }
        }
    }
}
