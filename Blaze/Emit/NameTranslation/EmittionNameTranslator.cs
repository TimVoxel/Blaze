using Blaze.Symbols;

namespace Blaze.Emit.NameTranslation
{
    internal enum StorageType
    {
        Strings, 
        Objects,
        Floats,
        Doubles,
        Enums, 
        Instances,
    }

    internal sealed class EmittionNameTranslator
    {
        public const string DEBUG_CHUNK_X = "10000000";
        public const string DEBUG_CHUNK_Z = "10000000";
        public const string TEMP = ".temp";
        public const string RETURN_TEMP_NAME = "return.value";

        private readonly Dictionary<Symbol, string> _emittionTranslations = new Dictionary<Symbol, string>();
        private readonly string _rootNamespace;

        public UUID MathEntity2 { get; }
        public UUID MathEntity1 { get; }
        public string Vars => $"{_rootNamespace}.vars";
        public string MainStorage => $"{_rootNamespace}:main";
        public string Const => "CONST";


        public EmittionNameTranslator(string rootNamespace)
        {
            _rootNamespace = rootNamespace;
            MathEntity1 = new UUID(1068730519, 377069937, 1764794166, -1230438844);
            MathEntity2 = new UUID(-1824770608, 1852200875, -1037488134, 520770809);
        }

        public string GetStorage(TypeSymbol type)
        {
            return MainStorage;
            /*
            if (type == TypeSymbol.String)
                return $"{_rootNamespace}:strings";
            else if (type == TypeSymbol.Object)
                return $"{_rootNamespace}:objects";
            else if (type == TypeSymbol.Float)
                return $"{_rootNamespace}:floats";
            else if (type == TypeSymbol.Double)
                return $"{_rootNamespace}:doubles";
            else if (type is EnumSymbol)
                return $"{_rootNamespace}:enums";
            else
                return $"{_rootNamespace}:instances";
            */
        }
        
        public string GetStorage(StorageType type)
        {
            return MainStorage;
            /*switch (type) 
            {
                case StorageType.Strings:
                    return $"{_rootNamespace}:strings";
                case StorageType.Objects:
                    return $"{_rootNamespace}:objects";
                case StorageType.Doubles:
                case StorageType.Floats:
                case StorageType.Instances:
                case StorageTypes
            }*/
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