using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EvitaDB.Client.Generators;

/// <summary>
/// Emits a concrete view class for every typed entity contract, plus a module initializer registering it
/// with <c>TypedViewRegistry</c>.
/// <remarks>
/// <para>
/// The generated class derives from <c>TypedEntityView</c>, which carries all of the behaviour, and
/// forwards each member to <c>Read</c>/<c>Write</c> by member name. Two things follow that the runtime
/// proxy cannot offer: members with a default interface implementation are simply <b>inherited</b> rather
/// than intercepted, and no reflection happens per call, so the result is trim- and AOT-safe.
/// </para>
/// <para>
/// The generator matches annotations by name, so it needs no reference to the driver assembly.
/// </para>
/// </remarks>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class TypedViewGenerator : IIncrementalGenerator
{
    private const string EntityAttribute = "EvitaEntityAttribute";
    private const string EntityRefAttribute = "EvitaEntityRefAttribute";
    private const string NotBoundAttribute = "NotBoundAttribute";
    private const string TypedNamespace = "EvitaDB.Client.Models.Typed";

    /// <summary>Mixins whose members the base class already implements.</summary>
    private static readonly string[] BaseProvidedInterfaces = ["ITypedEntity", "ITypedEntityEditor"];

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var contracts = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => ResolveContract(ctx)
            )
            .Where(static it => it is not null)
            .Select(static (it, _) => it!)
            .Collect();

        // records are bound positionally instead of proxied: they are immutable snapshots, so there is
        // nothing to intercept - the generated binder just constructs one from the fetched data
        var records = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is RecordDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => ResolveRecordContract(ctx)
            )
            .Where(static it => it is not null)
            .Select(static (it, _) => it!)
            .Collect();

        context.RegisterSourceOutput(contracts.Combine(records), static (productionContext, both) =>
        {
            var (interfaceModels, recordModels) = both;
            var emitted = new List<ContractModel>();
            var emittedRecords = new List<RecordContractModel>();

            foreach (var model in interfaceModels.Distinct(ContractModelComparer.Instance))
            {
                var source = Emit(model);
                if (source is null)
                {
                    continue;
                }

                productionContext.AddSource($"{model.ViewName}.g.cs", source);
                emitted.Add(model);
            }

            foreach (var model in recordModels.Distinct(RecordContractModelComparer.Instance))
            {
                productionContext.AddSource($"{model.BinderName}.g.cs", EmitRecordBinder(model));
                emittedRecords.Add(model);
            }

            if (emitted.Count > 0 || emittedRecords.Count > 0)
            {
                productionContext.AddSource(
                    "TypedViewRegistrations.g.cs", EmitRegistrations(emitted, emittedRecords));
            }
        });
    }

    /// <summary>
    /// Describes a record contract by its primary constructor: each parameter is read from the entity under
    /// its own name, which is also the name of the property the record generates.
    /// </summary>
    private static RecordContractModel? ResolveRecordContract(GeneratorSyntaxContext context)
    {
        if (context.Node is not RecordDeclarationSyntax declaration)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var annotated = symbol.GetAttributes().Any(it =>
            it.AttributeClass?.Name is EntityAttribute or EntityRefAttribute
            && it.AttributeClass.ContainingNamespace?.ToDisplayString() == TypedNamespace);

        if (!annotated)
        {
            return null;
        }

        // only a positional record can be bound - one declared with a body has no parameter list to map
        var constructor = symbol.InstanceConstructors
            .Where(it => it.Parameters.Length > 0)
            .OrderByDescending(it => it.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null)
        {
            return null;
        }

        var parameters = constructor.Parameters
            .Select(it => new ParameterModel(it.Name, it.Type.ToDisplayString(FullyQualified)))
            .ToImmutableArray();

        return new RecordContractModel(
            symbol.ToDisplayString(FullyQualified),
            BuildViewName(symbol).Replace("__View", "__RecordBinder"),
            parameters
        );
    }

    private static ContractModel? ResolveContract(GeneratorSyntaxContext context)
    {
        if (context.Node is not InterfaceDeclarationSyntax declaration)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var annotated = symbol.GetAttributes().Any(it =>
            it.AttributeClass?.Name is EntityAttribute or EntityRefAttribute
            && it.AttributeClass.ContainingNamespace?.ToDisplayString() == TypedNamespace);

        if (!annotated)
        {
            return null;
        }

        var members = new List<MemberModel>();

        foreach (var member in CollectMembers(symbol))
        {
            var model = DescribeMember(member);
            if (model is not null)
            {
                members.Add(model);
            }
        }

        return new ContractModel(
            symbol.ToDisplayString(FullyQualified),
            BuildViewName(symbol),
            members.ToImmutableArray()
        );
    }

    /// <summary>
    /// Walks the contract and every interface it extends, skipping the mixins the base class implements.
    /// </summary>
    private static IEnumerable<ISymbol> CollectMembers(INamedTypeSymbol contract)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in new[] { contract }.Concat(contract.AllInterfaces))
        {
            if (BaseProvidedInterfaces.Contains(type.Name))
            {
                continue;
            }

            foreach (var member in type.GetMembers())
            {
                // a member with a body is inherited from the interface - that is the whole point of the
                // generated path, so it must not be re-implemented here
                if (member.IsStatic || !member.IsAbstract)
                {
                    continue;
                }

                if (member.GetAttributes().Any(it => it.AttributeClass?.Name == NotBoundAttribute))
                {
                    continue;
                }

                var key = member switch
                {
                    IPropertySymbol property => $"P:{property.Name}",
                    IMethodSymbol method =>
                        $"M:{method.Name}({string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString()))})",
                    _ => null
                };

                if (key is not null && seen.Add(key))
                {
                    yield return member;
                }
            }
        }
    }

    private static MemberModel? DescribeMember(ISymbol member)
    {
        switch (member)
        {
            case IPropertySymbol { IsIndexer: false } property:
            {
                return new MemberModel(
                    MemberShape.Property,
                    property.Name,
                    property.Type.ToDisplayString(FullyQualified),
                    HasGetter: property.GetMethod is not null,
                    HasSetter: property.SetMethod is not null,
                    Parameters: ImmutableArray<ParameterModel>.Empty,
                    ReturnsVoid: false,
                    FluentReturnType: null
                );
            }

            case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
            {
                if (method.IsGenericMethod
                    || method.Parameters.Any(it => it.RefKind != RefKind.None))
                {
                    // out/ref parameters and generic methods have no meaningful binding
                    return null;
                }

                var fluent = !method.ReturnsVoid
                             && method.ContainingType is not null
                             && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.ContainingType);

                return new MemberModel(
                    MemberShape.Method,
                    method.Name,
                    method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(FullyQualified),
                    HasGetter: false,
                    HasSetter: false,
                    Parameters: method.Parameters
                        .Select(it => new ParameterModel(
                            it.Name, it.Type.ToDisplayString(FullyQualified)))
                        .ToImmutableArray(),
                    ReturnsVoid: method.ReturnsVoid,
                    FluentReturnType: fluent ? method.ReturnType.ToDisplayString(FullyQualified) : null
                );
            }

            default:
                return null;
        }
    }

    private static string? Emit(ContractModel model)
    {
        if (model.Members.Length == 0)
        {
            // nothing to implement; the base class plus inherited default members would suffice, but a
            // view with no members has no use either
            return null;
        }

        var body = new StringBuilder();

        body.AppendLine("// <auto-generated/>");
        body.AppendLine("#nullable enable");
        body.AppendLine();
        body.AppendLine("namespace EvitaDB.Client.Models.Typed.Generated;");
        body.AppendLine();
        body.AppendLine("/// <summary>Generated typed view. Do not edit.</summary>");
        body.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"EvitaDB.Client.Generators\", \"1\")]");
        body.AppendLine(
            $"public sealed class {model.ViewName} : global::{TypedNamespace}.TypedEntityView, {model.ContractType}");
        body.AppendLine("{");
        body.AppendLine($"    /// <summary>Creates the view over the given source.</summary>");
        body.AppendLine($"    public {model.ViewName}(");
        body.AppendLine($"        global::{TypedNamespace}.BoundTypedContract contract,");
        body.AppendLine("        object source) : base(contract, source)");
        body.AppendLine("    {");
        body.AppendLine("    }");

        foreach (var member in model.Members)
        {
            body.AppendLine();
            EmitMember(body, member);
        }

        body.AppendLine("}");
        return body.ToString();
    }

    private static void EmitMember(StringBuilder body, MemberModel member)
    {
        if (member.Shape == MemberShape.Property)
        {
            body.AppendLine($"    /// <inheritdoc />");
            body.Append($"    public {member.TypeName} {member.Name}");

            if (member is { HasGetter: true, HasSetter: false })
            {
                body.AppendLine($" => ({member.TypeName})Read(\"{member.Name}\")!;");
                return;
            }

            body.AppendLine();
            body.AppendLine("    {");
            if (member.HasGetter)
            {
                body.AppendLine($"        get => ({member.TypeName})Read(\"{member.Name}\")!;");
            }

            if (member.HasSetter)
            {
                body.AppendLine($"        set => Write(\"{member.Name}\", new object?[] {{ value }});");
            }

            body.AppendLine("    }");
            return;
        }

        var parameters = string.Join(
            ", ", member.Parameters.Select(it => $"{it.TypeName} {it.Name}"));
        var arguments = member.Parameters.Length == 0
            ? "null"
            : $"new object?[] {{ {string.Join(", ", member.Parameters.Select(it => it.Name))} }}";

        body.AppendLine($"    /// <inheritdoc />");

        if (member.ReturnsVoid)
        {
            body.AppendLine($"    public void {member.Name}({parameters})");
            body.AppendLine("    {");
            body.AppendLine($"        Write(\"{member.Name}\", {arguments});");
            body.AppendLine("    }");
            return;
        }

        if (member.FluentReturnType is not null)
        {
            body.AppendLine($"    public {member.FluentReturnType} {member.Name}({parameters})");
            body.AppendLine("    {");
            body.AppendLine($"        Write(\"{member.Name}\", {arguments});");
            body.AppendLine("        return this;");
            body.AppendLine("    }");
            return;
        }

        body.AppendLine(
            $"    public {member.TypeName} {member.Name}({parameters}) => " +
            $"({member.TypeName})Read(\"{member.Name}\", {arguments})!;");
    }

    private static string EmitRecordBinder(RecordContractModel model)
    {
        var body = new StringBuilder();

        body.AppendLine("// <auto-generated/>");
        body.AppendLine("#nullable enable");
        body.AppendLine();
        body.AppendLine("namespace EvitaDB.Client.Models.Typed.Generated;");
        body.AppendLine();
        body.AppendLine("/// <summary>Generated positional binder for a record contract. Do not edit.</summary>");
        body.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"EvitaDB.Client.Generators\", \"1\")]");
        body.AppendLine($"public static class {model.BinderName}");
        body.AppendLine("{");
        body.AppendLine("    /// <summary>Reads every constructor parameter and builds the record.</summary>");
        body.AppendLine($"    public static object Bind(");
        body.AppendLine($"        global::{TypedNamespace}.BoundTypedContract contract,");
        body.AppendLine("        object source)");
        body.AppendLine("    {");
        body.AppendLine($"        return new {model.ContractType}(");

        for (var i = 0; i < model.Parameters.Length; i++)
        {
            var parameter = model.Parameters[i];
            var comma = i == model.Parameters.Length - 1 ? "" : ",";
            body.AppendLine(
                $"            ({parameter.TypeName})contract.ReadByMember(" +
                $"\"{Capitalize(parameter.Name)}\", source, null)!{comma}");
        }

        body.AppendLine("        );");
        body.AppendLine("    }");
        body.AppendLine("}");
        return body.ToString();
    }

    /// <summary>
    /// A positional parameter is lower-cased while the property the record generates is not, and the
    /// contract is described by its properties.
    /// </summary>
    private static string Capitalize(string name) =>
        string.IsNullOrEmpty(name) || char.IsUpper(name[0])
            ? name
            : char.ToUpperInvariant(name[0]) + name.Substring(1);

    private static string EmitRegistrations(
        IEnumerable<ContractModel> models,
        IEnumerable<RecordContractModel> recordModels)
    {
        var body = new StringBuilder();

        body.AppendLine("// <auto-generated/>");
        body.AppendLine("#nullable enable");
        body.AppendLine();
        body.AppendLine("namespace EvitaDB.Client.Models.Typed.Generated;");
        body.AppendLine();
        body.AppendLine("/// <summary>Registers the generated views. Do not edit.</summary>");
        body.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"EvitaDB.Client.Generators\", \"1\")]");
        body.AppendLine("internal static class TypedViewRegistrations");
        body.AppendLine("{");
        body.AppendLine("    /// <summary>Runs before any user code touches the typed layer.</summary>");
        body.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        body.AppendLine("    internal static void Register()");
        body.AppendLine("    {");

        foreach (var model in models)
        {
            body.AppendLine(
                $"        global::{TypedNamespace}.TypedViewRegistry.Register(");
            body.AppendLine($"            typeof({model.ContractType}),");
            body.AppendLine($"            static (contract, source) => new {model.ViewName}(contract, source));");
        }

        foreach (var model in recordModels)
        {
            body.AppendLine(
                $"        global::{TypedNamespace}.TypedViewRegistry.Register(");
            body.AppendLine($"            typeof({model.ContractType}),");
            body.AppendLine($"            static (contract, source) => {model.BinderName}.Bind(contract, source));");
        }

        body.AppendLine("    }");
        body.AppendLine("}");
        return body.ToString();
    }

    /// <summary>
    /// Builds a collision-resistant view name from the contract's full name, since contracts of the same
    /// simple name may live in different namespaces or be nested in different types.
    /// </summary>
    private static string BuildViewName(INamedTypeSymbol symbol)
    {
        var qualified = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        var sanitized = new StringBuilder(qualified.Length + 6);

        foreach (var character in qualified)
        {
            sanitized.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        sanitized.Append("__View");
        return sanitized.ToString();
    }

    private static readonly SymbolDisplayFormat FullyQualified = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private enum MemberShape
    {
        Property,
        Method
    }

    private sealed record ParameterModel(string Name, string TypeName);

    private sealed record MemberModel(
        MemberShape Shape,
        string Name,
        string TypeName,
        bool HasGetter,
        bool HasSetter,
        ImmutableArray<ParameterModel> Parameters,
        bool ReturnsVoid,
        string? FluentReturnType);

    private sealed record ContractModel(
        string ContractType,
        string ViewName,
        ImmutableArray<MemberModel> Members);

    private sealed record RecordContractModel(
        string ContractType,
        string BinderName,
        ImmutableArray<ParameterModel> Parameters);

    private sealed class RecordContractModelComparer : IEqualityComparer<RecordContractModel>
    {
        internal static readonly RecordContractModelComparer Instance = new();

        public bool Equals(RecordContractModel? x, RecordContractModel? y) =>
            string.Equals(x?.ContractType, y?.ContractType, StringComparison.Ordinal);

        public int GetHashCode(RecordContractModel obj) => obj.ContractType.GetHashCode();
    }

    private sealed class ContractModelComparer : IEqualityComparer<ContractModel>
    {
        internal static readonly ContractModelComparer Instance = new();

        public bool Equals(ContractModel? x, ContractModel? y) =>
            string.Equals(x?.ContractType, y?.ContractType, StringComparison.Ordinal);

        public int GetHashCode(ContractModel obj) => obj.ContractType.GetHashCode();
    }
}
