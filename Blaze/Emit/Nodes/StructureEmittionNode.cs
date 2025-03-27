using Blaze.Symbols;

namespace Blaze.Emit.Nodes
{
    public abstract class StructureEmittionNode : EmittionNode {

        public IMemberSymbol? Symbol { get; }
        public string Name { get; }

        public string FullName
        {
            get
            {
                //We calculate the call name once
                //Then cache it and use the cached value


                if (Symbol == null)
                    return Name;

                return Symbol.GetFullName();
                /*
               

                if (_fullName != null)
                    return _fullName;

                var builder = new StringBuilder();
                var stack = new Stack<IMemberSymbol>();
                stack.Push(Symbol);

                var previous = Symbol.Parent;
                while (previous != null)
                {
                    if (previous is NamespaceSymbol ns && ns.IsGlobal)
                        break;
                    stack.Push(previous);
                    previous = previous.Parent;
                }

                while (stack.Any())
                {
                    builder.Append(stack.Pop().Name);

                    if (stack.Count > 0)
                        builder.Append("/");
                }

                _fullName = builder.ToString();
                return _fullName;
            
                */
            }
        }

        public StructureEmittionNode(IMemberSymbol? symbol, string name) : base()
        {
            Symbol = symbol;
            Name = name;
        }
    }
}
