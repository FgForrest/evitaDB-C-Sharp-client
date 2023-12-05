using System.Reflection;
using System.Text.RegularExpressions;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.QueryValidator.Serialization.Json.Binders;
using EvitaDB.QueryValidator.Serialization.Json.Converters;
using EvitaDB.QueryValidator.Serialization.Json.Resolvers;
using EvitaDB.QueryValidator.Serialization.Markdown;
using EvitaDB.QueryValidator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;

namespace EvitaDB.QueryValidator;

public static partial class Program
{
    private const string TempFolderName = "evita-query-validator";
    private const string QueryReplacementFileName = "evita-csharp-query-template.txt";
    private static readonly Regex TheQueryReplacement = QueryReplacementRegex();

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Converters =
        {
            new EntitySerializer(),
            new OrderedJsonSerializer(),
            new StripListSerializer(),
            new PaginatedListSerializer(),
            new DecimalConverter()
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
        string host = args.Length > 1 ? args[1] : throw new ArgumentException("Host is required!");
        string outputFormat = args.Length > 2 ? args[2] : throw new ArgumentException("Output format is required!");
        string? sourceVariable = args.Length > 3 ? args[3] : null;

        if (!File.Exists(QueryReplacementPath))
        {
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), TempFolderName));
            DownloadQueryTemplate();
        }

        string[] templateLines = File.ReadAllLines(QueryReplacementPath);

        string code = string.Join('\n', templateLines
            .Select(theLine =>
            {
                Match queryReplacementMatcher = TheQueryReplacement.Match(theLine);
                string result = theLine;
                if (queryReplacementMatcher.Success)
                {
                    result = queryCode;
                }
                result = result.Replace("#HOST#", host);
                return result;
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
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        using MemoryStream ms = new MemoryStream();
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
            Assembly assembly = Assembly.Load(ms.ToArray());
            try
            {
                Type? snippetClass = assembly.GetType("DynamicClass");
                MethodInfo? method = snippetClass?.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);

                if (snippetClass is not null && method is not null)
                {
                    var responseAndEntitySchema = ((EvitaResponse<ISealedEntity>? response, IEntitySchema entitySchema))
                        method.Invoke(null, new object?[] { FindCatalogName(queryCode) })!;
                    if (responseAndEntitySchema.response is not null)
                    {
                        string serializedOutput;
                        switch (outputFormat)
                        {
                            case "md":
                                serializedOutput = MarkdownConverter.GenerateMarkDownTable(
                                    responseAndEntitySchema.entitySchema,
                                    responseAndEntitySchema.response.Query,
                                    responseAndEntitySchema.response
                                );
                                break;
                            case "json" when sourceVariable is not null:
                            {
                                object? value =
                                    ResponseSerializerUtils.ExtractValueFrom(responseAndEntitySchema.response,
                                        sourceVariable.Split('.'));
                                string stringSerialized = JsonConvert.SerializeObject(value, JsonSettings);
                                serializedOutput = WrapSerializedOutputInCodeBlock("json", stringSerialized);
                                break;
                            }
                            case "string":
                            {
                                object? theValue;
                                if (string.IsNullOrEmpty(sourceVariable))
                                {
                                    theValue = responseAndEntitySchema.response;
                                }
                                else
                                {
                                    theValue = ResponseSerializerUtils.ExtractValueFrom(
                                        responseAndEntitySchema.response,
                                        sourceVariable.Split('.'));
                                }

                                string json;
                                if (theValue is not null)
                                {
                                    json = theValue is IPrettyPrintable pp ? pp.PrettyPrint() : theValue.ToString()!;
                                }
                                else
                                {
                                    json = "";
                                }
                                serializedOutput = WrapSerializedOutputInCodeBlock("md", json);
                                break;
                            }
                            default:
                                throw new ArgumentException("Bad combination of output format and source variable!");
                        }

                        Console.WriteLine(serializedOutput);
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

    private static string FindCatalogName(string text)
    {
        int firstDoubleQuote = text.IndexOf('"');
        int secondDoubleQuote = text.IndexOf('"', firstDoubleQuote + 1);
        return text.Substring(firstDoubleQuote + 1, secondDoubleQuote - firstDoubleQuote - 1);
    }

    private static void DownloadQueryTemplate()
    {
        using HttpClient client = new HttpClient();
        HttpResponseMessage response = client
            .GetAsync(
                "https://raw.githubusercontent.com/FgForrest/evitaDB-C-Sharp-client/master/EvitaDB.QueryValidator/csharp_query_template.txt")
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using Stream contentStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult(),
            stream = new FileStream(QueryReplacementPath, FileMode.Create);
        contentStream.CopyTo(stream);
    }

    private static string WrapSerializedOutputInCodeBlock(string codeBlockLang, string serializedOutput)
    {
        return $"```{codeBlockLang}\n{serializedOutput}\n```";
    }

    [GeneratedRegex(@"^(\s*)#QUERY#.*$", RegexOptions.Singleline)]
    private static partial Regex QueryReplacementRegex();
}
