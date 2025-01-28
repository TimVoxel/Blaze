using Blaze.Binding;
using Blaze.Symbols.BuiltIn;
using System.Collections.Immutable;

namespace Blaze.Symbols
{
    internal abstract class BuiltInNamespace
    {
        public NamespaceSymbol Symbol { get; }

        public static readonly MinecraftNamespace Minecraft = new MinecraftNamespace();
        public static readonly BlazeNamespace Blaze = new BlazeNamespace();

        public static IEnumerable<NamespaceSymbol> GetAll() => GetAllWraps().Select(s => s.Symbol);

        private static IEnumerable<BuiltInNamespace> GetAllWraps()
        {
            yield return Minecraft;
            yield return Blaze;
        }

        public BuiltInNamespace(string name)
        {
            Symbol = NamespaceSymbol.CreateBuiltIn(name, null);
        }

        public BuiltInNamespace(string name, BuiltInNamespace parent)
        {
            Symbol = NamespaceSymbol.CreateBuiltIn(name, parent.Symbol);
            parent.Symbol.Members.Add(Symbol);
        }

        protected FunctionSymbol Function(string name, TypeSymbol returnType, params ParameterSymbol[] parameters)
        {
            var function = new FunctionSymbol(name, Symbol, parameters.ToImmutableArray(), returnType, false, false, AccessModifier.Public, null);
            Symbol.TryDeclareFunction(function);
            return function;
        }

        protected FunctionSymbol PrivateFunction(string name, TypeSymbol returnType, params ParameterSymbol[] parameters)
        {
            var function = new FunctionSymbol(name, Symbol, parameters.ToImmutableArray(), returnType, false, false, AccessModifier.Private, null);
            Symbol.TryDeclareFunction(function);
            return function;
        }

        protected NamedTypeSymbol Class(string name, ConstructorSymbol? constructor, NamedTypeSymbol? baseType = null, bool isAbstract = false) 
        {
            var classSymbol = new NamedTypeSymbol(name, baseType, Symbol, constructor, isAbstract);
            Symbol.Members.Add(classSymbol);
            return classSymbol;
        }

        protected NamedTypeSymbol Class(string name, NamedTypeSymbol? baseType, bool isAbstract, params (string name, TypeSymbol type)[] fields)
        {
            ParameterSymbol[] parameters = new ParameterSymbol[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                var tuple = fields[i];
                parameters[i] = Parameter(tuple.name, tuple.type);
            }

            var constructor = Constructor(parameters);
            var result = Class(name, constructor, baseType, isAbstract);
            
            for (int i = 0; i < fields.Length; i++)
            {
                var tuple = fields[i]; 
                AddField(result, tuple.name, tuple.type);
            }

            constructor.FunctionBody = AssignFieldsBlock(result, parameters);
            return result;
        }

        protected NamedTypeSymbol AbstractClass(string name, NamedTypeSymbol? baseType = null) => Class(name, null, baseType, true);
        
        protected EnumSymbol Enum(string name, bool isInt)
        {
            var enumSymbol = new EnumSymbol(Symbol, name, isInt);
            Symbol.Members.Add(enumSymbol);
            return enumSymbol;
        }

        protected EnumSymbol DeclareEnumMember(string name, EnumSymbol enumSymbol, string underlyingValue)
        {
            var member = new StringEnumMemberSymbol(enumSymbol, name, underlyingValue);
            enumSymbol.Members.Add(member);
            return enumSymbol;
        }

        protected EnumSymbol DeclareEnumMember(string name, EnumSymbol enumSymbol, int underlyingValue)
        {
            var member = new IntEnumMemberSymbol(enumSymbol, name, underlyingValue);
            enumSymbol.Members.Add(member);
            return enumSymbol;
        }

        protected ConstructorSymbol Constructor(params ParameterSymbol[] parameters)
        {
            var constructor = new ConstructorSymbol(parameters.ToImmutableArray());
            return constructor;
        }

        protected FieldSymbol AddField(NamedTypeSymbol parent, string name, TypeSymbol type)
        {
            var field = new FieldSymbol(name, parent, type);
            parent.Members.Add(field);
            return field;
        }

        protected FieldSymbol AddField(NamespaceSymbol parent, string name, TypeSymbol type)
        {
            var field = new FieldSymbol(name, parent, type);
            parent.Members.Add(field);
            return field;
        }

        protected BoundBlockStatement Block(params BoundStatement[] boundStatements)
        {
            var block = new BoundBlockStatement(boundStatements.ToImmutableArray());
            return block;
        }

        protected BoundBlockStatement AssignFieldsBlock(NamedTypeSymbol type, params ParameterSymbol[] parametersInOrder)
        {
            var fields = type.Fields;
            var blockBuilder = ImmutableArray.CreateBuilder<BoundStatement>();

            if (fields.Count() != parametersInOrder.Length)
                throw new Exception("Parameter count does not equal field count");

            var i = 0;
            foreach (var field in fields)
            {
                var param = parametersInOrder[i];
                if (field.Type != param.Type)
                    throw new Exception($"Field {field.Name} and param {param.Name} do not share the same type");

                var boundThisExpression = new BoundThisExpression(type);
                var fieldExpression = new BoundFieldAccessExpression(boundThisExpression, field);
                var paramExpression = new BoundVariableExpression(param);
                var assignment = new BoundAssignmentExpression(fieldExpression, paramExpression);
                var statement = new BoundExpressionStatement(assignment);
                blockBuilder.Add(statement);
                i++;
            }

            var block = new BoundBlockStatement(blockBuilder.ToImmutable());
            return block;
        }


        protected ParameterSymbol Parameter(string name, TypeSymbol type)
            => new ParameterSymbol(name, type, name.GetHashCode());
    }
}
