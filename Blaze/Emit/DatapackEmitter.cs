using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.IO;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Text;

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
                {
                    BuildFunction(functionsDirectory, function);
                }
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
            if (function.SubFunctions.Any())
            {
                var subFunctionDirectory = Path.Combine(functionsDirectory, function.Name);
                Directory.CreateDirectory(subFunctionDirectory);
                foreach (var subFunction in function.SubFunctions)
                    BuildFunction(subFunctionDirectory, subFunction);
            }
            else
            {
                var functionFile = Path.Combine(functionsDirectory, function.Name + ".mcfunction");
                using (var streamWriter = new StreamWriter(functionFile))
                    streamWriter.Write(function.Body);
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
    }

    public class FunctionEmittion
    {
        public string Name { get; }
        public string Body { get; }
        public ImmutableArray<FunctionEmittion> SubFunctions { get; }

        public FunctionEmittion(string name, string body, ImmutableArray<FunctionEmittion> subFunctions)
        {
            Name = name;
            Body = body;
            SubFunctions = subFunctions;
        }
    }

    internal class DatapackEmitter
    {
        private readonly BoundProgram _program;
        private readonly CompilationConfiguration _configuration;

        public DatapackEmitter(BoundProgram program, CompilationConfiguration configuration)
        {
            _program = program;
            _configuration = configuration;
        }

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CompilationConfiguration? configuration)
        {
            if (program.Diagnostics.Any() || configuration == null)
                return program.Diagnostics;

            var emitter = new DatapackEmitter(program, configuration);
            emitter.BuildPacks();

            return program.Diagnostics;
        }

        private void BuildPacks()
        {
            var emittionsBuilder = ImmutableArray.CreateBuilder<FunctionEmittion>();
            foreach (var function in _program.Functions)
            {
                var functionEmittion = EmitFunction(function.Key, function.Value);
                emittionsBuilder.Add(functionEmittion);
            }

            var datapack = new Datapack(_configuration, emittionsBuilder.ToImmutable());
            datapack.Build();
        }

        private FunctionEmittion EmitFunction(FunctionSymbol function, BoundStatement bodyBlock)
        {
            var bodyBuidler = new StringBuilder();
            var children = ImmutableArray.CreateBuilder<FunctionEmittion>();
            EmitStatement(bodyBlock, bodyBuidler, children);
            return new FunctionEmittion(function.Name, bodyBuidler.ToString(), children.ToImmutable());
        }

        private void EmitStatement(BoundStatement node, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    EvaluateBlockStatement((BoundBlockStatement)node, body, children);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EvaluateExpressionStatement((BoundExpressionStatement)node, body, children);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    EmitVariableDeclarationStatement((BoundVariableDeclarationStatement)node, body, children);
                    break;
                case BoundNodeKind.IfStatement:
                    EvaluateIfStatement((BoundIfStatement)node, body, children);
                    break;
                case BoundNodeKind.WhileStatement:
                    EvaluateWhileStatement((BoundWhileStatement)node, body, children);
                    break;
                case BoundNodeKind.DoWhileStatement:
                    EvaluateDoWhileStatement((BoundDoWhileStatement)node, body, children);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private void EvaluateBlockStatement(BoundBlockStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EmitVariableDeclarationStatement(BoundVariableDeclarationStatement node, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //TODO: Add vars scoreboard in a load function

            var initializer = node.Initializer;
            var name = $"*{node.Variable.Name}";

            if (initializer is BoundLiteralExpression literal)
            {
                //int literal       -> scoreboard players set *v integers <value>
                //string literal    -> data modify storage strings string <SOURCE> [<sourcePath>]
                //bool literal      -> scoreboard players set *v bools <value>

                if (literal.Type == TypeSymbol.String)
                {
                    var value = (string) literal.Value;
                    var command = $"data modify storage strings {name} set value {value}";
                    body.AppendLine(command);
                }
                else
                {
                    var value = literal.Type == TypeSymbol.Int ? (int)literal.Value
                                        : ((bool)literal.Value ? 1 : 0);

                    var command = $"scoreboard players set {name} vars {value}";
                    body.AppendLine(command);
                }
            }
            else if (initializer is BoundVariableExpression v)
            {
                //int, bool literal -> scoreboard players operation *this vars = *other vars
                //string literal    -> data modify storage strings *this set from storage strings *other

                var other = v.Variable.Name;

                if (v.Type == TypeSymbol.String)
                {
                    var command = $"data modify storage strings {name} set from storage strings {other}";
                    body.AppendLine(command);
                }
                else
                {
                    var command = $"scoreboard players operation {name} vars = {other} vars";
                    body.AppendLine(command);
                }
            }
            /*
            switch (initializer.Kind)
            {
                case BoundNodeKind.AssignmentExpression:
                    WriteAssignmentExpression((BoundAssignmentExpression)node, writer);
                    break;
                case BoundNodeKind.UnaryExpression:
                    WriteUnaryExpression((BoundUnaryExpression)node, writer);
                    break;
                case BoundNodeKind.BinaryExpression:
                    WriteBinaryExpression((BoundBinaryExpression)node, writer);
                    break;
                case BoundNodeKind.CallExpression:
                    WriteCallExpression((BoundCallExpression)node, writer);
                    break;
                case BoundNodeKind.ConversionExpression:
                    WriteConversionExpression((BoundConversionExpression)node, writer);
                    break;
            }*/
        }

        private void EvaluateIfStatement(BoundIfStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EvaluateWhileStatement(BoundWhileStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EvaluateDoWhileStatement(BoundDoWhileStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EmitComment(string message, StringBuilder builder)
        {
            builder.AppendLine($"#{message}");
        }

        private void EmitReturnStatement(BoundReturnStatement statement, StringBuilder builder)
        {
            builder.AppendLine("say return statement");
        }

        private void EmitExpressionStatement(BoundExpressionStatement statement, StringBuilder builder)
        {
            switch (statement.Expression.Kind)
            {
                case BoundNodeKind.CallExpression:
                    EmitCallExpressionStatement((BoundCallExpression)statement.Expression, builder);
                    break;
                default:
                    builder.AppendLine("say expression statement");
                    break;
            }
        }

        private void EmitCallExpressionStatement(BoundCallExpression call, StringBuilder builder)
        {
            if (TryEmitBuiltInFunction(call, builder))
                return;

            //TODO: IMPLEMENT NAMESPACES
            builder.AppendLine($"function ns:{call.Function.Name}");
        }

        private bool TryEmitBuiltInFunction(BoundCallExpression call, StringBuilder builder)
        { 
            if (call.Function == BuiltInFunction.RunCommand)
            {
                var costant = call.Arguments[0].ConstantValue;
              
                if (costant == null)
                {
                    //HACK
                    throw new Exception($"run_command only uses constant values");
                }

                var message = (string) costant.Value;
                builder.AppendLine(message);
                return true;
                
            }
            if (call.Function == BuiltInFunction.Print)
            {
                var message = call.Arguments[0].ToString().Replace("\"", "\\\"");
                var convertedMessage = "{\"text\":\"§e" + message + "\"}";
                builder.AppendLine($"tellraw @a {convertedMessage}");
                return true;
            }
            return false;
        }

    }
}
