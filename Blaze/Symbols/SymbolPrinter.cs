using Blaze.IO;

namespace Blaze.Symbols
{
    internal static class SymbolPrinter
    {
        public static void WriteTo(Symbol symbol, TextWriter writer)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Parameter:
                case SymbolKind.LocalVariable:
                case SymbolKind.GlobalVariable:
                    WriteVariable((VariableSymbol)symbol, writer);
                    break;
                case SymbolKind.Type:
                    WriteType((TypeSymbol)symbol, writer);
                    break;
                case SymbolKind.Function:
                    WriteFunction((FunctionSymbol)symbol, writer);
                    break;
                default:
                    throw new Exception($"Unexpected symbol kind {symbol.Kind}");
            }
        }

        private static void WriteFunction(FunctionSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword("function ");
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation("(");

            for (int i = 0; i < symbol.Parameters.Length; i++)
            {
                if (i > 0)
                    writer.WritePunctuation(", ");

                symbol.Parameters[i].WriteTo(writer);
            }

            writer.WritePunctuation(")");

            if (symbol.ReturnType != TypeSymbol.Void)
            {
                writer.WritePunctuation(" : ");
                symbol.ReturnType.WriteTo(writer);
            }     
        }

        //private static void WriteLocalVariable(LocalVariableSymbol symbol, TextWriter writer) => WriteVariable(symbol, writer);
        //private static void WriteGlobalVariable(GlobalVariableSymbol symbol, TextWriter writer) => WriteVariable(symbol, writer);
        //private static void WriteParameter(ParameterSymbol symbol, TextWriter writer) => WriteVariable(symbol, writer);

        private static void WriteVariable(VariableSymbol symbol, TextWriter writer)
        {
            symbol.Type.WriteTo(writer);
            writer.Write(" ");
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteType(TypeSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.Name);
        }
    }
}
