using Blaze.Symbols;
using System.Collections.Immutable;
using System.Text;

namespace Blaze.Emit
{
    public class FunctionEmittion : Emittion
    {
        private string? _callName;
        private int _loopCount = 0;
        private int _ifCount = 0;
        private int _elseCount = 0;
        
        public FunctionSymbol Symbol { get; }
        public string Body { get; private set; }
        public string CleanUp { get; private set; }
        private bool IsSub { get; }

        public string CallName
        {
            get
            {
                //If the function is a fabricated sub function, 
                //It's call name would be different to one that is
                //Contained in the symbol

                if (!IsSub)
                    return Symbol.AddressName;

                if (_callName != null)
                    return _callName;

                var builder = new StringBuilder();
                var stack = new Stack<IMemberSymbol>();

                var previous = Symbol.Parent;
                while (previous != null)
                {
                    if (previous.IsRoot)
                        break;
                    stack.Push(previous);
                    previous = previous.Parent;
                }
                while (stack.Any())
                {
                    builder.Append(stack.Pop().Name + "/");
                }

                builder.Append(Name);
                _callName = builder.ToString();
                return _callName;
            }
        }

        public List<FunctionEmittion> Children { get; }

        private FunctionEmittion(string name, FunctionSymbol symbol, bool isSub = false) : base(name)
        {
            IsSub = isSub;
            Symbol = symbol;
            Body = string.Empty;
            CleanUp = string.Empty;
            Children = new List<FunctionEmittion>();
        }

        private FunctionEmittion(string fullName, ConstructorSymbol symbol, bool isSub = false) : base(symbol.Name)
        {
            _callName = fullName;
            IsSub = isSub;
            Symbol = symbol;
            Body = string.Empty;
            CleanUp = string.Empty;
            Children = new List<FunctionEmittion>();
        }

        public static FunctionEmittion FromSymbol(FunctionSymbol symbol)
        {
            return new FunctionEmittion(symbol.Name, symbol);
        }

        public static FunctionEmittion FromConstructor(ConstructorSymbol constructorSymbol, string fullName)
        {
            return new FunctionEmittion(fullName, constructorSymbol, false);
        }

        public static FunctionEmittion Init(NamespaceSymbol globalNamespace)
        {
            FunctionSymbol init = new FunctionSymbol("init", globalNamespace, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, true, false, null);
            return new FunctionEmittion(init.Name, init, false);
        }
        public static FunctionEmittion Tick(NamespaceSymbol globalNamespace)
        {
            FunctionSymbol tick = new FunctionSymbol("tick", globalNamespace, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, false, true, null);
            return new FunctionEmittion(tick.Name, tick, false);
        }

        public static FunctionEmittion CreateSub(FunctionEmittion parent, SubFunctionKind kind)
        {
            string name = kind switch
            {
                SubFunctionKind.If => parent.GetFreeSubIfName(),
                SubFunctionKind.Else => parent.GetFreeSubElseName(),
                SubFunctionKind.Loop => parent.GetFreeSubLoopName(),
                 
                _ => throw new Exception($"Unexpected sub function kind {kind}")
            };
            var emittion = new FunctionEmittion(name, parent.Symbol, true);
            parent.Children.Add(emittion);
            return emittion;
        }

        public void Append(string text)
        {
            Body += text;
        }

        public void AppendLine(string line)
        {
            Body += line + Environment.NewLine;
        }

        public void AppendLine() => Body += Environment.NewLine;
        public void AppendComment(string text) => AppendLine($"#{text}");
        public void AppendMacro(string text) => AppendLine($"${text}");

        public void AppendCleanUp(string line)
        {
            if (!CleanUp.Contains(line))
                CleanUp += line + Environment.NewLine;
        }

        private string GetFreeSubIfName()
        {
            _ifCount++;
            return $"{Name}_sif{_ifCount}"; 
        }

        private string GetFreeSubElseName()
        {
            _elseCount++;
            return $"{Name}_sel{_elseCount}";
        }

        private string GetFreeSubLoopName()
        {
            _loopCount++;
            return $"{Name}_sw{_loopCount}";
        }

        /*
        public string GetCallName()
        {
            var stack = new Stack<FunctionEmittion>();
            var current = Parent;

            stack.Push(this);

            while (current != null)
            {
                stack.Push(current);
                current = Parent;
            }

            var callName = new StringBuilder("ns:");
            while (stack.Any())
            {
                var isLast = stack.Count == 1;
                var thisEmittion = stack.Pop();
                var ending = isLast ? string.Empty : "/";
                callName.Append($"{thisEmittion.Name}{ending}");
            }
            return callName.ToString();
        }
        */
    }
}