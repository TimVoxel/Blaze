using Blaze.Binding;
using Blaze.Diagnostics;
using System.Collections.Immutable;
using Mono.Cecil;
using Blaze.Symbols;

namespace Blaze.Emit
{
    internal static class ILEmitter
    {
        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, string moduleName, string[] references, string outputPath)
        {
            if (program.Diagnostics.Any())
                return program.Diagnostics;

            List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
            DiagnosticBag result = new DiagnosticBag();

            foreach (string reference in references)
            {
                try
                {
                    AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(reference);
                    assemblies.Add(assembly);
                }
                catch (BadImageFormatException)
                {
                    result.ReportInvalidReference(reference);
                }
            }

            var builtInTypes = new List<(TypeSymbol type, string MetaDataName)>()
            {
                (TypeSymbol.Object, "System.Object"),
                (TypeSymbol.Bool, "System.Boolean"),
                (TypeSymbol.Int, "System.Int32"),
                (TypeSymbol.String, "System.String"),
                (TypeSymbol.String, "System.Void")
            };

            Dictionary<TypeSymbol, TypeReference> knownTypes = new Dictionary<TypeSymbol, TypeReference>();

            foreach (var (type, metaDataName) in builtInTypes)
            {
                TypeDefinition[] foundTypes = assemblies.SelectMany(a => a.Modules)
                                                        .SelectMany(m => m.Types)
                                                        .Where(t => t.FullName == metaDataName)
                                                        .ToArray();

                /*
                if (foundTypes.Length == 1)
                {

                }
                else if (foundTypes.Length == 0)
                {
                    result.ReportMissingBuiltInType(type);
                }
                else
                {
                    result.ReportBuiltInTypeAmbiguous(type, foundTypes);
                }*/
            }

            if (result.Any())
                return result.ToImmutableArray();

            AssemblyNameDefinition assemblyname = new AssemblyNameDefinition(moduleName, new Version(1, 0));
            AssemblyDefinition assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyname, moduleName, ModuleKind.Console);
            

            TypeDefinition typeDefinition = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed);
            assemblyDefinition.MainModule.Types.Add(typeDefinition);

            //MethodDefinition main = new MethodDefinition("Main", MethodAttributes.Static, );

            assemblyDefinition.Write(outputPath);

            return result.ToImmutableArray();
        }
    }
}
