using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.IO;
using Blaze.Symbols;
using Mono.Cecil.Cil;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.IO;
using System.Net.WebSockets;

namespace Blaze.Emit
{
    public sealed class Datapack
    {
        private readonly CompilationConfiguration _configuration;

        private readonly List<(string name, string body)> _functions = new List<(string name, string body)>();

        public Datapack(CompilationConfiguration configuration)
        {
            _configuration = configuration;
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

        public void AddFunction(string name, string body)
        {
            _functions.Add((name, body));
        }
    }


    internal class DatapackEmitter
    {
        private readonly BoundProgram _program;
        private readonly Datapack _datapack;

        public DatapackEmitter(BoundProgram program, CompilationConfiguration configuration)
        {
            _program = program;
            _datapack = new Datapack(configuration);
            InitializePack();
        }

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CompilationConfiguration? configuration)
        {
            if (program.Diagnostics.Any() || configuration == null)
                return program.Diagnostics;

            var emitter = new DatapackEmitter(program, configuration);
            emitter.BuildPacks();

            return program.Diagnostics;
        }

        private void BuildPacks() => _datapack.Build();

        private void InitializePack()
        {
            foreach (var function in _program.Functions.Keys)
            {
                var body = _program.Functions[function];
                var translatedFunction = TranslateFunction(function, body);
                _datapack.AddFunction(translatedFunction.name, translatedFunction.body);
            }
            
        }

        private (string name, string body) TranslateFunction(FunctionSymbol function, BoundBlockStatement bodyBlock)
        {
            string name = function.Name;
            string body = "aboba";
            return (name, body);
        }
    }
}
