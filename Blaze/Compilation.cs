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
        public CompilationConfiguration? Configuration { get; }
        
        private Compilation(CompilationConfiguration? configuration, params SyntaxTree[] trees)
        {
            SyntaxTrees = trees.ToImmutableArray();
            Configuration = configuration;
        }

        public static Compilation Create(CompilationConfiguration configuration, params SyntaxTree[] syntaxTrees) => new Compilation(configuration, syntaxTrees);
        public static Compilation CreateScript(params SyntaxTree[] syntaxTrees) => new Compilation(null, syntaxTrees);

        public IEnumerable<Symbol> GetSymbols() => GetSymbolsInNamespace(GlobalScope.GlobalNamespace);

        private IEnumerable<Symbol> GetSymbolsInNamespace(NamespaceSymbol ns)
        {
            if (ns.IsBuiltIn)
                yield break;

            if (!ns.IsGlobal)
                yield return ns;

            foreach (var child in ns.Members)
            {
                if (child is Symbol symbol)
                {
                    switch (symbol.Kind)
                    {
                        case SymbolKind.Namespace:
                            var memberNamespace = (NamespaceSymbol)child;
                            foreach (var childSymbol in GetSymbolsInNamespace(memberNamespace))
                                yield return childSymbol;
                            break;
                        default:
                            yield return symbol;
                            break;
                    }
                }
            }
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
            var program = GetProgram();

            foreach (var ns in program.Namespaces.Values)
            {
                ns.WriteTo(writer);
            }
        }

        public void EmitTree(FunctionSymbol function, TextWriter writer)
        {
            var program = GetProgram();

            foreach (var ns in program.Namespaces.Values)
            {
                if (ns.Functions.TryGetValue(function, out var body))
                    return;

                function.WriteTo(writer);
                writer.WriteLine();
                if (body != null)
                    body.WriteTo(writer);
            }
        }

        public ImmutableArray<Diagnostic> Emit()
        {
            var program = GetProgram();
            return DatapackEmitter.Emit(program, Configuration);
        }

        private void EmitControlFlowGraph(BoundProgram program)
        {
            var targetNamespace = program.Namespaces.Values.First();
            var loweredBody = Lowerer.DeepLower(targetNamespace.Functions.Last().Value);
            var controlFlowGraphStatement = loweredBody.Statements.Any() && targetNamespace.Functions.Any()
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
