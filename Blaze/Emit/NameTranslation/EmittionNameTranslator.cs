using Blaze.Symbols;

namespace Blaze.Emit.NameTranslation
{
    internal class EmittionNameTranslator
    {
        public const string TEMP = ".temp";
        public const string RETURN_TEMP_NAME = "return.value";

        private readonly Dictionary<Symbol, string> _emittionTranslations = new Dictionary<Symbol, string>();
        private readonly string _rootNamespace;

        public string Vars => $"{_rootNamespace}.vars";
        public string Const => "CONST";

        public EmittionNameTranslator(string rootNamespace)
        {
            _rootNamespace = rootNamespace;
        }

        public string GetStorage(TypeSymbol type)
        {
            if (type == TypeSymbol.String)
                return $"{_rootNamespace}:strings";
            else if (type == TypeSymbol.Object)
                return $"{_rootNamespace}:objects";
            else if (type is EnumSymbol)
                return $"{_rootNamespace}:enums";
            else
                return $"{_rootNamespace}:instances";
        }

        public string GetNamespaceFieldPath(NamespaceSymbol namespaceSymbol)
        {
            if (_emittionTranslations.ContainsKey(namespaceSymbol))
            {
                var name = _emittionTranslations[namespaceSymbol];
                return name;
            }
            else
            {
                var name = $"*{namespaceSymbol.GetFullName()}";
                _emittionTranslations.Add(namespaceSymbol, name);
                return name;
            }
        }

        public string GetCallLink(FunctionEmittion emittion)
        {
            return $"{_rootNamespace}:{emittion.CallName}";
        }

        public string GetCallLink(FunctionSymbol symbol)
        {
            return $"{_rootNamespace}:{symbol.AddressName}";
        }

        public string GetVariableName(VariableSymbol localVariable)
        {
            if (_emittionTranslations.ContainsKey(localVariable))
            {
                var name = _emittionTranslations[localVariable];
                return name;
            }

            var scoreName = $"*{localVariable.Name}";
            _emittionTranslations.Add(localVariable, scoreName);
            return scoreName;
        }

        public void Unregister(VariableSymbol localVariable) => _emittionTranslations.Remove(localVariable);
    }
}