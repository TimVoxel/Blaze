using Blaze.Syntax_Nodes;
using System.Text;

namespace Blaze.Symbols
{
    public sealed class NamespaceSymbol : Symbol, IMemberSymbol
    {
        public List<IMemberSymbol> Members { get; }
        public List<NamespaceSymbol> Usings { get; }
        public IMemberSymbol? Parent { get; }
        public NamespaceDeclarationSyntax? Declaration { get; }

        public FunctionSymbol? LoadFunction => Functions.SingleOrDefault(f => f.IsLoad);
        public FunctionSymbol? TickFunction => Functions.SingleOrDefault(f => f.IsTick);

        public bool IsGlobal { get; }
        public bool IsBuiltIn { get; }

        public IEnumerable<NamespaceSymbol> NestedNamespaces => Members.OfType<NamespaceSymbol>();
        public IEnumerable<FunctionSymbol> Functions => Members.OfType<FunctionSymbol>();
        public IEnumerable<FieldSymbol> Fields => Members.OfType<FieldSymbol>();
        public IEnumerable<NamedTypeSymbol> NamedTypes => Members.OfType<NamedTypeSymbol>();
        public IEnumerable<EnumSymbol> Enums => Members.OfType<EnumSymbol>();

        public IEnumerable<NamespaceSymbol> AllNestedNamespaces
        {
            get
            {
                foreach (var nested in NestedNamespaces)
                {
                    foreach (var nested2 in nested.AllNestedNamespaces)
                        yield return nested2;

                    yield return nested;
                }
            }
        }

        public IEnumerable<FunctionSymbol> AllFunctions 
        {
            get
            {
                foreach (var nested in AllNestedNamespaces)
                {
                    foreach (var function in nested.AllFunctions)
                        yield return function;
                }

                foreach (var function in Functions)
                    yield return function;
            }
        }

        public IEnumerable<NamedTypeSymbol> AllNamedTypes 
        {
            get
            {
                foreach (var nested in AllNestedNamespaces)
                {
                    foreach (var type in nested.AllNamedTypes)
                        yield return type;
                }

                foreach (var type in NamedTypes)
                    yield return type;
            }
        }

        public override SymbolKind Kind => SymbolKind.Namespace;

        public NamespaceSymbol(string name, NamespaceSymbol? parent, NamespaceDeclarationSyntax? declaration, bool isGlobal = false, bool isBuiltIn = false) : base(name)
        {
            Parent = parent;
            Members = new List<IMemberSymbol>();
            Declaration = declaration;
            IsGlobal = isGlobal;
            IsBuiltIn = isBuiltIn;
            Usings = new List<NamespaceSymbol>();
        }

        public static NamespaceSymbol CreateGlobal(string name)
        {
            return new NamespaceSymbol(name, null, null, true);
        }

        public static NamespaceSymbol CreateBuiltIn(string name, NamespaceSymbol? parent)
        {
            return new NamespaceSymbol(name, parent, null, false, true);
        }

        public bool TryDeclareNested(NamespaceSymbol namespaceSymbol)
        {
            if (NestedNamespaces.FirstOrDefault(n => n.Name == namespaceSymbol.Name) != null)
                return false;

            Members.Add(namespaceSymbol);
            return true;
        }

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            if (Functions.FirstOrDefault(f => f.Name == function.Name.ToLower()) != null)
                return false;

            Members.Add(function);
            return true;
        }

        public FunctionSymbol? TryLookupFunction(string name, bool includeUsings = true)
        {
            var function = Members.OfType<FunctionSymbol>().FirstOrDefault(f => f.Name == name.ToLower());
            var parent = (NamespaceSymbol?) Parent;

            if (function == null && parent != null)
                return parent.TryLookupFunction(name.ToLower());

            return function;
        }

        public T? TryLookup<T>(string name, bool includeUsings = true) where T : IMemberSymbol
        {
            var member = Members.OfType<T>().FirstOrDefault(m => m.Name == name);
            if (member == null)
            {
                if (includeUsings)
                {
                    foreach (var usedNamespace in Usings)
                    {
                        member = usedNamespace.TryLookup<T>(name, true);
                        if (member != null)
                            return member;
                    }
                }

                if (Parent != null)
                {
                    var parent = (NamespaceSymbol)Parent;
                    return parent.TryLookup<T>(name, includeUsings);
                }
                else return member;
            }
            return member;
        }

        public NamedTypeSymbol? TryLookupClass(string name, bool includeUsings = true)
        {
            var classSymbol = NamedTypes.FirstOrDefault(cl => cl.Name == name);
            
            if (classSymbol == null)
            {
                if (includeUsings)
                {
                    foreach (var usedNamespace in Usings)
                    {
                        classSymbol = usedNamespace.TryLookupClass(name, true);
                        if (classSymbol != null)
                            return classSymbol;
                    }
                }

                if (Parent != null)
                {
                    var parent = (NamespaceSymbol) Parent;
                    return parent.TryLookupClass(name, includeUsings);
                }
                else return classSymbol;
            }
            return classSymbol;
        }

        public NamespaceSymbol? TryLookupDirectChild(string name)
        {
            return NestedNamespaces.FirstOrDefault(n => n.Name == name);
        }

        public string GetFullName()
        {
            var nameBuilder = new StringBuilder();
            var nameStack = new Stack<string>();
            IMemberSymbol? previous = this;

            while (previous != null)
            {
                if (previous is NamespaceSymbol ns && ns.IsGlobal)
                    break;
                nameStack.Push(previous.Name);
                previous = previous.Parent;
            }

            while (nameStack.Any())
            {
                nameBuilder.Append(nameStack.Pop());
                if (nameStack.Count >= 1)
                    nameBuilder.Append(".");
            }
            return nameBuilder.ToString();
        }
    }
}
