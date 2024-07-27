using Blaze.Binding;
using Blaze.Symbols;
using System.Text;

namespace Blaze.Emit.NameTranslation
{
    internal abstract class EmittionTranslation { }
    internal class SingleName : EmittionTranslation
    {
        public string Value { get; }

        public SingleName(string value)
        {
            Value = value;
        }
    }

    internal class ObjectNameSet : EmittionTranslation
    {
        public Dictionary<Symbol, EmittionTranslation> _localTranslations = new Dictionary<Symbol, EmittionTranslation>();

        public string ObjectName { get; }

        public ObjectNameSet(VariableSymbol? reference, NamedTypeSymbol type)
        {
            if (reference == null)
                ObjectName = "*obj_temp";
            else
                ObjectName = $"*{reference.Name}";
        }
    }

    internal class EmittionNameTranslator
    {
        public const string TEMP = ".temp";
        public const string RETURN_TEMP_NAME = "return.value";
        public const string CONSTRUCTORS_NAMESPACE = "__constructors";

        private readonly Dictionary<Symbol, EmittionTranslation> _emittionTranslations = new Dictionary<Symbol, EmittionTranslation>();
        private readonly string _rootNamespace;

        public EmittionNameTranslator(string rootNamespace)
        {
            _rootNamespace = rootNamespace;
        }

        public string GetStorage(TypeSymbol type)
        {
            if (type == TypeSymbol.String)
                return "strings";
            else if (type == TypeSymbol.Object)
                return "objects";
            else
                return "instances";
        }

        public string GetConstructorCallName(NamedTypeSymbol constructorType, out bool isGenerated)
        {
            if (_emittionTranslations.ContainsKey(constructorType.Constructor))
            {
                var name = (SingleName) _emittionTranslations[constructorType.Constructor];
                isGenerated = true;
                return name.Value;
            }
            else
            {
                string callName = $"{CONSTRUCTORS_NAMESPACE}/{constructorType.GetFullName().ToLower()}";
                _emittionTranslations.Add(constructorType.Constructor, new SingleName(callName));
                isGenerated = false;
                return callName;
            }
        }

        public string GetNamespaceFieldPath(NamespaceSymbol namespaceSymbol)
        {
            if (_emittionTranslations.ContainsKey(namespaceSymbol))
            {
                var name = (SingleName)_emittionTranslations[namespaceSymbol];
                return name.Value;
            }
            else
            {
                var name = $"*{namespaceSymbol.GetFullName()}";
                var singleName = new SingleName(name);
                _emittionTranslations.Add(namespaceSymbol, singleName);
                return name;
            }
        }

        public string GetConstructorCallLink(ConstructorSymbol constructorSymbol)
        {
            var name = (SingleName) _emittionTranslations[constructorSymbol];
            return $"{_rootNamespace}:{name.Value}";
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
                var name = (SingleName)_emittionTranslations[localVariable];
                return name.Value;
            }

            var scoreName = $"*{localVariable.Name}";
            var newName = new SingleName(scoreName);
            _emittionTranslations.Add(localVariable, newName);
            return scoreName;
        }

        public void Unregister(VariableSymbol localVariable) => _emittionTranslations.Remove(localVariable);
    }
}