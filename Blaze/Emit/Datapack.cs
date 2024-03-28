using Blaze.IO;
using System.Collections.Immutable;

namespace Blaze.Emit
{
    public sealed class Datapack
    {
        private readonly CompilationConfiguration _configuration;

        private readonly ImmutableArray<FunctionEmittion> _functions;

        public Datapack(CompilationConfiguration configuration, ImmutableArray<FunctionEmittion> functionEmittions)
        {
            _configuration = configuration;
            _functions = functionEmittions;
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


            //3. Generate all functions
            //TODO: Add namespaces

            if (_functions.Any())
            {
                var namespaceDirectory = Path.Combine(dataDirectory, "ns");
                var functionsDirectory = Path.Combine(namespaceDirectory, "functions");
                Directory.CreateDirectory(functionsDirectory);

                foreach (var function in _functions)
                    BuildFunction(functionsDirectory, function);
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


        private void BuildFunction(string functionsDirectory, FunctionEmittion function)
        {
            var functionFile = Path.Combine(functionsDirectory, function.Name + ".mcfunction");
            using (var streamWriter = new StreamWriter(functionFile))
            {
                streamWriter.Write(function.Body);
                if (!string.IsNullOrEmpty(function.CleanUp))
                {
                    streamWriter.WriteLine("#Clean up commands");
                    streamWriter.Write(function.CleanUp);
                }
            }

            foreach (var child in function.Children)
                BuildFunction(functionsDirectory, child);
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
