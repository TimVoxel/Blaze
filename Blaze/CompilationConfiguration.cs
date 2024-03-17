using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blaze
{
    public sealed class CompilationConfiguration
    {
        public string Name { get; }
        public string? Description { get; }
        public string? Author { get; }
        public int Format { get; }
        //public ImmutableArray<string>? LoadFunctions { get; }
        //public ImmutableArray<string>? TickFunctions { get; }

        public ImmutableArray<string> OutputFolders { get; }

        [JsonConstructor]
        public CompilationConfiguration(string name,
                                        int format,
                                        ImmutableArray<string> outputFolders,
                                        string? description = null,
                                        string? author = null)//,
                                        //ImmutableArray<string>? loadFunctions = null,
                                        //ImmutableArray<string>? tickFunctions = null)
        {
            Name = name;
            Format = format;
            OutputFolders = outputFolders;
            Description = description;
            Author = author;
            //LoadFunctions = loadFunctions;
            //TickFunctions = tickFunctions;
        }

        public static CompilationConfiguration? FromJson(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open))
            {
                return JsonSerializer.Deserialize<CompilationConfiguration>(fileStream);
            }
        }
    }
}
