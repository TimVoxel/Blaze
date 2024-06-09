using Blaze.IO;
using System.Collections.Immutable;

namespace Blaze.Emit
{
    public sealed class Datapack
    {
        private readonly CompilationConfiguration _configuration;

        private readonly ImmutableArray<FunctionNamespaceEmittion> _namespaces;

        public Datapack(CompilationConfiguration configuration, ImmutableArray<FunctionNamespaceEmittion> namespaceEmittions)
        {
            _configuration = configuration;
            _namespaces = namespaceEmittions;
        }

        public void Build()
        {
            //1. Create a pack folder
            var packName = _configuration.Name;
            var outputDirectory = _configuration.OutputFolders.First();
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

            //3. Generate namespaces all functions
            if (_namespaces.Any())
            {
                var rootNamespaceDirectory = Path.Combine(dataDirectory, _configuration.RootNamespace);
                Directory.CreateDirectory(rootNamespaceDirectory);

                var functionsDirectory = Path.Combine(rootNamespaceDirectory, "functions");
                Directory.CreateDirectory(functionsDirectory);

                foreach (var namespaceEmittion in _namespaces)
                    BuildFunctionNamespace(functionsDirectory, namespaceEmittion);
            }

            //4. Copy the result pack to all of the output paths
            foreach (var outputPath in _configuration.OutputFolders)
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
        }

        private void BuildFunctionNamespace(string parentDirectory, FunctionNamespaceEmittion namespaceEmittion)
        {
            //1. Generate sub directory of the namespace inside the needed directory
            //2. Generate all functions in the namespace and all child namespaces

            var namespaceDirectory = Path.Combine(parentDirectory, namespaceEmittion.Name);
            Directory.CreateDirectory(namespaceDirectory);

            foreach (var function in namespaceEmittion.Functions)
                BuildFunction(namespaceDirectory, function);

            foreach (var child in namespaceEmittion.Children)
                BuildFunctionNamespace(namespaceDirectory, child);
        }

        private void BuildFunction(string targetDirectory, FunctionEmittion emittion)
        {
            var functionFile = Path.Combine(targetDirectory, emittion.Name + ".mcfunction");
            using (var streamWriter = new StreamWriter(functionFile))
            {
                streamWriter.Write(emittion.Body);
                if (!string.IsNullOrEmpty(emittion.CleanUp))
                {
                    streamWriter.WriteLine("#Clean up commands");
                    streamWriter.Write(emittion.CleanUp);
                }
            }

            foreach (var child in emittion.Children)
                BuildFunction(targetDirectory, child);
        }

        private void WriteMcMeta(TextWriter textWriter)
        {
            var description = _configuration.Description;
            var author = _configuration.Author;
            var format = _configuration.Format;

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
