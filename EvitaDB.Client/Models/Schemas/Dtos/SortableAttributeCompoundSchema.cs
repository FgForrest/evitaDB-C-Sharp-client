using System.Collections.Immutable;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class SortableAttributeCompoundSchema : ISortableAttributeCompoundSchema
{
    public string Name { get; }
    public string? Description { get; }
    public IDictionary<NamingConvention, string?> NameVariants { get; }
    public string? GetNameVariant(NamingConvention namingConvention) => NameVariants.TryGetValue(namingConvention, out string? name) ? name : null;

    public string? DeprecationNotice { get; }
    public IList<AttributeElement> AttributeElements { get; }
    private bool? MemoizedLocalized { get; set; }

    internal static SortableAttributeCompoundSchema InternalBuild(
        string name,
        string? description,
        string? deprecationNotice,
        IList<AttributeElement> attributeElements
    )
    {
        return new SortableAttributeCompoundSchema(
            name,
            NamingConventionHelper.Generate(name),
            description,
            deprecationNotice,
            attributeElements
        );
    }

    internal static SortableAttributeCompoundSchema InternalBuild(
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        IList<AttributeElement> attributeElements
    )
    {
        return new SortableAttributeCompoundSchema(
            name,
            nameVariants,
            description,
            deprecationNotice,
            attributeElements
        );
    }

    private SortableAttributeCompoundSchema(
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        string? deprecationNotice,
        IEnumerable<AttributeElement> attributeElements
    )
    {
        Name = name;
        NameVariants = nameVariants.ToImmutableDictionary();
        Description = description;
        DeprecationNotice = deprecationNotice;
        AttributeElements = attributeElements.ToImmutableList();
    }

    public bool IsLocalized(Func<string, AttributeSchema> attributeSchemaProvider)
    {
        MemoizedLocalized ??= AttributeElements
            .Any(it =>
            {
                IAttributeSchema attributeSchema = attributeSchemaProvider.Invoke(it.AttributeName);
                Assert.NotNull(attributeSchema, "Attribute `" + it.AttributeName + "` schema not found!");
                return attributeSchema.Localized();
            });

        return MemoizedLocalized.Value;
    }
}
