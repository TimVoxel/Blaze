using Blaze.Binding;
using Blaze.Symbols.BuiltIn;
using System.Collections.Immutable;

namespace Blaze.Symbols
{
    internal abstract class BuiltInNamespace
    {
        protected NamespaceSymbol Symbol { get; }

        public static readonly MinecraftNamespace Minecraft = new MinecraftNamespace();

        public static IEnumerable<NamespaceSymbol> GetAll() => GetAllWraps().Select(s => s.Symbol);

        private static IEnumerable<BuiltInNamespace> GetAllWraps()
        {
            yield return Minecraft;
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
            var function = new FunctionSymbol(name, Symbol, parameters.ToImmutableArray(), returnType, false, false, null);
            Symbol.TryDeclareFunction(function);
            return function;
        }

        protected NamedTypeSymbol Class(string name, ConstructorSymbol constructor) 
        {
            var classSymbol = new NamedTypeSymbol(name, Symbol, constructor);
            Symbol.Members.Add(classSymbol);
            return classSymbol;
        }

        protected ConstructorSymbol Constructor(params ParameterSymbol[] parameters)
        {
            var constructor = new ConstructorSymbol(parameters.ToImmutableArray());
            return constructor;
        }

        protected FieldSymbol Field(NamedTypeSymbol parent, string name, TypeSymbol type)
        {
            var field = new FieldSymbol(name, parent, type);
            parent.Members.Add(field);
            return field;
        }

        protected FieldSymbol Field(NamespaceSymbol parent, string name, TypeSymbol type)
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
            => new ParameterSymbol(name, type);
    }
}
