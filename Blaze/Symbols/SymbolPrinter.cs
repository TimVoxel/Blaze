﻿using Blaze.IO;

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
                case SymbolKind.Namespace:
                    WriteNamespace((NamespaceSymbol)symbol, writer);
                    break;
                case SymbolKind.NamedType:
                    WriteNamedType((NamedTypeSymbol)symbol, writer);
                    break;
                case SymbolKind.ArrayType:
                    WriteArrayType((ArrayTypeSymbol)symbol, writer);
                    break;
                case SymbolKind.Field:
                    WriteField((FieldSymbol)symbol, writer);
                    break;
                case SymbolKind.ArrayInstance:
                    WriteVariable((VariableSymbol) symbol, writer);
                    break;
                case SymbolKind.Enum:
                    WriteEnum((EnumSymbol)symbol, writer);
                    break;
                case SymbolKind.EnumMember:
                    WriteEnumMember((EnumMemberSymbol)symbol, writer);
                    break;
                case SymbolKind.EmittionVariable:
                    WriteEmittionVariable((EmittionVariableSymbol)symbol, writer);
                    break;
                default:
                    throw new Exception($"Unexpected symbol kind {symbol.Kind}");
            }
        }

        private static void WriteEmittionVariable(EmittionVariableSymbol emittionVariableSymbol, TextWriter writer)
        {
            writer.WriteIdentifier(emittionVariableSymbol.SaveName);
        }

        private static void WriteEnumMember(EnumMemberSymbol symbol, TextWriter writer)
        {
            writer.WriteIdentifier(symbol.Name);
        }

        private static void WriteEnum(EnumSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword("enum ");
            writer.WriteIdentifier(symbol.GetFullName());
        }

        private static void WriteField(FieldSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword("field ");
            writer.WriteIdentifier(symbol.GetFullName());
        }

        private static void WriteNamespace(NamespaceSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword("namespace ");
            writer.WriteIdentifier(symbol.GetFullName());
        }

        private static void WriteNamedType(NamedTypeSymbol symbol, TextWriter writer)
        {
            writer.WriteIdentifier(symbol.GetFullName());
        }

        private static void WriteArrayType(ArrayTypeSymbol symbol, TextWriter writer)
        {
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation("[");
            for (int i = 0; i < symbol.Rank - 1; i++)
                writer.WritePunctuation(",");
            writer.WritePunctuation("]");
        }

        private static void WriteFunction(FunctionSymbol symbol, TextWriter writer)
        {
            if (symbol.IsLoad)
                writer.WriteKeyword("load ");
            if (symbol.IsTick)
                writer.WriteKeyword("tick ");
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
