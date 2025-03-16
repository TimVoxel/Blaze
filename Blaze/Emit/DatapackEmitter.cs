using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.Emit.Nodes;
using Blaze.IO;
using System.Collections.Immutable;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {   
        public Datapack Datapack { get; }

        public DatapackEmitter(Datapack datapack)
        {
            Datapack = datapack;
        }

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CompilationConfiguration? configuration)
        {
            if (program.Diagnostics.Any() || configuration == null)
                return program.Diagnostics;

            var builder = new DatapackBuilder(program, configuration);
            var datapack = builder.BuildDatapack();
            var emitter = new DatapackEmitter(datapack);

            var diagnostics = emitter.Emit();
            return diagnostics;
        }    

        public ImmutableArray<Diagnostic> Emit()
        {
            //1. Create a pack folder

            var packName = Datapack.Configuration.Name;
            var outputDirectory = Datapack.Configuration.OutputFolders.First();
            var packDirectory = Path.Combine(outputDirectory, packName);

            if (Directory.Exists(packDirectory))
            {
                //HACK: should return a diagnostic instead
                Console.WriteLine($"pack {packDirectory} already exists, overwriting it");
                Directory.Delete(packDirectory, true);
            }

            Directory.CreateDirectory(packDirectory);

            //2. Create pack.mcmeta
            var packMcMetaPath = Path.Combine(packDirectory, "pack.mcmeta");
            using (var streamWriter = new StreamWriter(packMcMetaPath))
                WriteMcMeta(streamWriter);

            var dataDirectory = Path.Combine(packDirectory, "data");
            Directory.CreateDirectory(dataDirectory);

            //3. Generate namespaces and functions
            if (Datapack.Namespaces.Any())
            {
                var rootNamespaceDirectory = Path.Combine(dataDirectory, Datapack.Configuration.RootNamespace);
                Directory.CreateDirectory(rootNamespaceDirectory);

                var functionsDirectory = Path.Combine(rootNamespaceDirectory, "function");
                Directory.CreateDirectory(functionsDirectory);

                foreach (var namespaceEmittion in Datapack.Namespaces)
                    GenerateNamespace(functionsDirectory, namespaceEmittion);

                GenerateFunction(functionsDirectory, Datapack.InitFunction);
                GenerateFunction(functionsDirectory, Datapack.TickFunction);
            }

            //4. Create the tags directory and generate tick and load function tags
            var minecraftNamespace = Path.Combine(dataDirectory, "minecraft");
            Directory.CreateDirectory(minecraftNamespace);

            var tagsDirectory = Path.Combine(minecraftNamespace, "tags");
            Directory.CreateDirectory(tagsDirectory);
            var functionTags = Path.Combine(tagsDirectory, "function");
            Directory.CreateDirectory(functionTags);

            using (var streamWriter = new StreamWriter(Path.Combine(functionTags, "load.json")))
            {
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\t\"values\": [");
                streamWriter.WriteLine($"\t\t\"{Datapack.Configuration.RootNamespace}:init\"");
                streamWriter.WriteLine("\t]");
                streamWriter.WriteLine("}");
            }
            using (var streamWriter = new StreamWriter(Path.Combine(functionTags, "tick.json")))
            {
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\t\"values\": [");
                streamWriter.WriteLine($"\t\t\"{Datapack.Configuration.RootNamespace}:tick\"");
                streamWriter.WriteLine("\t]");
                streamWriter.WriteLine("}");
            }

            //5. Copy the result pack to all of the output paths
            foreach (var outputPath in Datapack.Configuration.OutputFolders)
            {
                if (outputPath == outputDirectory)
                    continue;

                var destination = Path.Combine(outputPath, packName);

                if (Directory.Exists(destination))
                {
                    //HACK: should return a diagnostic instead
                    Console.WriteLine($"pack {destination} already exists, overwriting it");
                    Directory.Delete(destination, true);
                }

                DirectoryExtensions.Copy(packDirectory, destination);
            }

            return Datapack.Diagnostics;
        }

        private void GenerateNamespace(string parentDirectory, NamespaceEmittionNode namespaceEmittion)
        {
            //1. Generate sub directory of the namespace inside the needed directory
            //2. Generate all functions in the namespace and all child namespaces

            var namespaceDirectory = Path.Combine(parentDirectory, namespaceEmittion.Name);
            Directory.CreateDirectory(namespaceDirectory);

            foreach (var function in namespaceEmittion.Functions)
                GenerateFunction(namespaceDirectory, function);

            foreach (var child in namespaceEmittion.NestedNamespaces)
                GenerateNamespace(namespaceDirectory, child);
        }

        private void GenerateFunction(string targetDirectory, MinecraftFunction emittion)
        {
            var functionFile = Path.Combine(targetDirectory, emittion.Name + ".mcfunction");
            using (var streamWriter = new StreamWriter(functionFile))
            {
                foreach (var node in emittion.Content)
                    EmitTextNode(streamWriter, node);
            }

            if (emittion.SubFunctions != null)
                foreach (var child in emittion.SubFunctions)
                    GenerateFunction(targetDirectory, child);
        }

        private void EmitTextNode(StreamWriter writer, TextEmittionNode node)
        {
            switch (node.Kind)
            {
                case EmittionNodeKind.EmptyTrivia:
                    break;
                case EmittionNodeKind.TextBlock:
                    {
                        var textBlock = (TextBlockEmittionNode)node;
                        foreach (var subNode in textBlock.Lines)
                            EmitTextNode(writer, subNode);

                        break;
                    }
                default:
                    writer.WriteLine(node.Text);
                    break;
            }
        }

        private void WriteMcMeta(TextWriter textWriter)
        {
            var description = Datapack.Configuration.Description;
            var author = Datapack.Configuration.Author;
            var format = Datapack.Configuration.Format;

            //opening
            textWriter.WriteLine("{");
            textWriter.WriteLine("\t\"pack\": {");

            //format
            textWriter.WriteLine($"\t\t\"pack_format\": {format},");

            //description
            var hasDescription = description != null;
            var hasAuthor = author != null;

            textWriter.WriteLine("\t\t\"description\": [");
            if (hasDescription)
            {
                textWriter.WriteLine($"\t\t\t\"{description}\",");
            }

            if (hasAuthor)
            {
                textWriter.Write("\t\t\t\"");
                if (hasDescription)
                    textWriter.Write("\\n");
                textWriter.WriteLine($"Created by §e{author}\",");
            }

            textWriter.Write("\t\t\t\"");
            if (hasAuthor || hasDescription)
                textWriter.Write("\\n");

            textWriter.WriteLine("Created using the §6Blaze Compiler\"");
            textWriter.WriteLine("\t\t]");

            //closing
            textWriter.WriteLine("\t}");
            textWriter.WriteLine("}");
        }
    }
}