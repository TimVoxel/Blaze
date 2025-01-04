using Blaze.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Blaze.Emit.Nodes
{
    public sealed class MinecraftFunction : StructureEmittionNode
    {
        private int _loopCount = 0;
        private int _ifCount = 0;
        private int _elseCount = 0;

        public SubFunctionKind? SubFunctionKind { get; }

        public List<TextEmittionNode> Content { get; }
        public List<MinecraftFunction>? SubFunctions { get; private set; }
        
        public override EmittionNodeKind Kind => EmittionNodeKind.MinecraftFunction;

        private string? _callName;
        public string CallName
        {
            get
            {
                //If the function is a fabricated sub function, 
                //It's call name would be different to one that is
                //Contained in the symbol

                if (Symbol == null)
                    throw new Exception("Function emittion without a function symbol");

                if (_callName != null)
                    return _callName;

                if (SubFunctionKind == null)
                {
                    FunctionSymbol functionSymbol = (FunctionSymbol)Symbol;
                    _callName = functionSymbol.AddressName;
                    return _callName;
                }
                else
                {
                    var builder = new StringBuilder();
                    var stack = new Stack<IMemberSymbol>();

                    var previous = Symbol.Parent;
                    while (previous != null)
                    {
                        if (previous is NamespaceSymbol ns && ns.IsGlobal)
                            break;
                        stack.Push(previous);
                        previous = previous.Parent;
                    }

                    while (stack.Any())
                        builder.Append(stack.Pop().Name + "/");

                    builder.Append(Name);
                    _callName = builder.ToString();
                    return _callName;
                }
            }
        }

        public MinecraftFunction(string name, IMemberSymbol symbol, SubFunctionKind? subFunctionKind) : base(symbol, name)
        {
            SubFunctionKind = subFunctionKind;
            Content = new List<TextEmittionNode>();
        }

        public static MinecraftFunction Init(NamespaceSymbol globalNamespace)
        {
            var init = new FunctionSymbol("init", globalNamespace, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, true, false, AccessModifier.Private, null);
            return new MinecraftFunction(init.Name, init, null);
        }

        public static MinecraftFunction Tick(NamespaceSymbol globalNamespace)
        {
            var tick = new FunctionSymbol("tick", globalNamespace, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, false, true, AccessModifier.Private, null);
            return new MinecraftFunction(tick.Name, tick, null);
        }

        public void AddCommand(CommandNode command) => Content.Add(command);
        public void AddLineBreak() => Content.Add(TextTriviaNode.LineBreak());
        public void AddComment(string text) => Content.Add(TextTriviaNode.Comment(text));

        //Temporary
        public void AddCommand(string command, bool isCleanUp = false) => Content.Add(new TextCommand(command, isCleanUp));
        public void AddMacro(string command) => Content.Add(new TextCommand($"${command}", false));

        public CommandNode GetCall(string rootNamespace) => new TextCommand($"function {rootNamespace}:{FullName}", false);

        public MinecraftFunction CreateSub(SubFunctionKind kind)
        {
            if (SubFunctions == null)
                SubFunctions = new List<MinecraftFunction>();

            var name = string.Empty;
            switch (kind)
            {
                case Emit.SubFunctionKind.If:
                    name = $"{Name}_sif{_ifCount++}";
                    break;
                case Emit.SubFunctionKind.Else:
                    name = $"{Name}_sel{_elseCount++}";
                    break;
                case Emit.SubFunctionKind.Loop:
                    name = $"{Name}_sl{_loopCount++}";
                    break;
                default:
                    throw new Exception($"Unexpected sub function kind {Kind}");
            }

            Debug.Assert(Symbol != null);
            var sub = new MinecraftFunction(name, Symbol, kind);
            SubFunctions.Add(sub);
            return sub;
        }

        public MinecraftFunction CreateSubNamed(string name)
        {
            if (SubFunctions == null)
                SubFunctions = new List<MinecraftFunction>();

            var fullName = $"{Name}_{name}";

            Debug.Assert(Symbol != null);
            var sub = new MinecraftFunction(fullName, Symbol, Emit.SubFunctionKind.Misc);
            SubFunctions.Add(sub);
            return sub;
        }
    }

    /*

    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        enum SubAction 
        {
            Add,
            Remove,
            Reset
            
        }
           
        internal ScoreboardPlayersCommand()
        {

        }
    }*/
}
