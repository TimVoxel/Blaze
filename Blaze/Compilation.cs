using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.Emit;
using Blaze.Lowering;
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
        public CompilationConfiguration? Configuration { get; }

        private Compilation(CompilationConfiguration? configuration, params SyntaxTree[] trees)
        {
            SyntaxTrees = trees.ToImmutableArray();
            Configuration = configuration;
        }

        public static Compilation Create(CompilationConfiguration configuration, params SyntaxTree[] syntaxTrees) => new Compilation(configuration, syntaxTrees);
        public static Compilation CreateScript(params SyntaxTree[] syntaxTrees) => new Compilation(null, syntaxTrees);


        public IEnumerable<Symbol> GetSymbols()
        {
            foreach (FunctionSymbol function in Functions)
                yield return function;

            foreach (VariableSymbol variable in Variables)
                yield return variable;
        }

        private BoundProgram GetProgram() => Binder.BindProgram(GlobalScope);
 
        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
        {
            var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);
            var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();

            if (diagnostics.Any())
                return new EvaluationResult(diagnostics, null);

            var program = GetProgram();

            //EmitControlFlowGraph(program);

            if (program.Diagnostics.Any())
                return new EvaluationResult(program.Diagnostics.ToImmutableArray(), null);

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate();
            return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.MainFunction != null)
                EmitTree(GlobalScope.MainFunction, writer);
        }

        public void EmitTree(FunctionSymbol function, TextWriter writer)
        {
            var program = GetProgram();

            if (!program.Functions.TryGetValue(function, out BoundStatement? body))
                return;

            function.WriteTo(Console.Out);
            Console.WriteLine();
            body.WriteTo(Console.Out);
        }

        public ImmutableArray<Diagnostic> Emit()
        {
            var program = GetProgram();
            return DatapackEmitter.Emit(program, Configuration);
        }

        private void EmitControlFlowGraph(BoundProgram program)
        {
            var loweredBody = Lowerer.DeepLower(program.Functions.Last().Value);
            var controlFlowGraphStatement = loweredBody.Statements.Any() && program.Functions.Any()
                ? loweredBody
                : null;

            if (controlFlowGraphStatement == null)
                return;

            var cfg = ControlFlowGraph.Create(controlFlowGraphStatement);

            var appPath = Environment.GetCommandLineArgs()[0];
            var appDirectory = Path.GetDirectoryName(appPath);
            if (appDirectory != null)
            {
                var cfgPath = Path.Combine(appDirectory, "cfg.dot");
                using (var writer = new StreamWriter(cfgPath))
                    cfg.WriteTo(writer);
            }
        }
    }
}
