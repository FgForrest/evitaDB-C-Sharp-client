﻿using System.Reflection;
using System.Text.RegularExpressions;
using Client.DataTypes;
using Client.Models;
using Client.Models.Data;
using Client.Models.Data.Structure;
using Client.Models.ExtraResults;
using QueryValidator.Serialization.Json.Converters;
using QueryValidator.Serialization.Json.Binders;
using QueryValidator.Serialization.Json.Resolvers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;
using QueryValidator.Utils;

namespace QueryValidator;

public static partial class Program
{
    private static readonly Regex TheQueryReplacement = ReplacementRegex();
    private const string TempFolderName = "evita-query-validator";

    private const string QueryReplacementFileName = "evita-csharp-query-template.txt";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Converters =
        {
            new EntitySerializer(), 
            new OrderedJsonSerializer(), 
            new StripListSerializer(), 
            new PaginatedListSerializer()
        },
        TypeNameHandling = TypeNameHandling.None,
        ContractResolver = new OrderPropertiesResolver(),
        DefaultValueHandling = DefaultValueHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        Formatting = Formatting.Indented,
        SerializationBinder = new AllowedSerializationBinder(
            typeof(IEntityClassifier),
            typeof(IEntityClassifierWithParent),
            typeof(Hierarchy),
            typeof(LevelInfo),
            typeof(PaginatedList<>),
            typeof(StripList<>)
        )
    };

    private static readonly string QueryReplacementPath =
        Path.Combine(Path.GetTempPath(), TempFolderName, QueryReplacementFileName);

    public static void Main(string[] args)
    {
        string queryCode = args.Length > 0 ? args[0] : throw new ArgumentException("Query code is required!");
        string outputFormat = args.Length > 1 ? args[1] : throw new ArgumentException("Output format is required!");
        string? sourceVariable = args.Length > 2 ? args[2] : null;

        if (!File.Exists(QueryReplacementPath))
        {
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), TempFolderName));
            DownloadQueryTemplate();
        }

        string[] templateLines = File.ReadAllLines(QueryReplacementPath);

        var code = string.Join('\n', templateLines
            .Select(theLine =>
            {
                Match replacementMatcher = TheQueryReplacement.Match(theLine);
                return replacementMatcher.Success ? queryCode : theLine;
            }));

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

        string assemblyName = $"DynamicAssembly_{Guid.NewGuid()}.dll";
        string baseDir = AppContext.BaseDirectory;
        string runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        MetadataReference[] references =
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Console.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Globalization.dll")),
            MetadataReference.CreateFromFile(Path.Combine(baseDir, "Newtonsoft.Json.dll")),
            MetadataReference.CreateFromFile(Path.Combine(baseDir, "Google.Protobuf.dll")),
            MetadataReference.CreateFromFile(Path.Combine(baseDir, "Grpc.Net.Client.dll")),
            MetadataReference.CreateFromFile(Path.Combine(baseDir, "Grpc.Core.Api.dll")),
            MetadataReference.CreateFromFile(Path.Combine(baseDir, "EvitaDB.Client.dll"))
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] {syntaxTree},
            references: references,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        using var ms = new MemoryStream();
        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                Console.WriteLine($"{diagnostic.Severity}: {diagnostic.GetMessage()}");
            }
        }
        else
        {
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            try
            {
                var snippetClass = assembly.GetType("DynamicClass");
                var method = snippetClass?.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);

                if (snippetClass is not null && method is not null)
                {
                    EvitaResponse<SealedEntity>? response = (EvitaResponse<SealedEntity>?) method.Invoke(null, null);
                    if (response is not null)
                    {
                        switch (outputFormat)
                        {
                            case "md":
                                // TODO: Implement markdown output
                                break;
                            case "json" when sourceVariable is not null:
                            {
                                object? value = ResponseSerializerUtils.ExtractValueFrom(response, sourceVariable.Split('.'));
                                var stringSerialized = JsonConvert.SerializeObject(value, JsonSettings);
                                Console.WriteLine(WrapSerializedOutputInCodeBlock(stringSerialized));
                                break;
                            }
                            default:
                                throw new ArgumentException("Bad combination of output format and source variable!");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("No data returned!");
                    }
                }
                else
                {
                    Console.WriteLine("No entry point found");
                }
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine($"Exception: {ex.InnerException}\n{ex.InnerException?.StackTrace}");
            }
        }
    }

    private static void DownloadQueryTemplate()
    {
        using var client = new HttpClient();
        var response = client.GetAsync("https://evitadb.io/download/evita-csharp-query-template.txt").GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using Stream contentStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult(),
            stream = new FileStream(QueryReplacementPath, FileMode.Create);
        contentStream.CopyTo(stream);
    }

    private static string WrapSerializedOutputInCodeBlock(string serializedOutput)
    {
        return $"```json\n{serializedOutput}\n```";
    }

    [GeneratedRegex("^(\\s*)#QUERY#.*$", RegexOptions.Singleline)]
    private static partial Regex ReplacementRegex();
}