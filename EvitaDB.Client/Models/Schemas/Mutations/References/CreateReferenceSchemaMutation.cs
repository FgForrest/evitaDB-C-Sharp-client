using Client.DataTypes;
using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.References;

public class CreateReferenceSchemaMutation : IReferenceSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public Cardinality? Cardinality { get; }
    public string ReferencedEntityType { get; }
    public bool ReferencedEntityTypeManaged { get; }
    public string? ReferencedGroupType { get; }
    public bool ReferencedGroupTypeManaged { get; }
    public bool Indexed { get; }
    public bool Faceted { get; }

    public CreateReferenceSchemaMutation(string name, string? description, string? deprecationNotice,
        Cardinality? cardinality, string referencedEntityType, bool referencedEntityTypeManaged,
        string? referencedGroupType, bool referencedGroupTypeManaged, bool indexed, bool faceted)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Reference, referencedEntityType);
        Name = name;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Cardinality = cardinality;
        ReferencedEntityType = referencedEntityType;
        ReferencedEntityTypeManaged = referencedEntityTypeManaged;
        ReferencedGroupType = referencedGroupType;
        ReferencedGroupTypeManaged = referencedGroupTypeManaged;
        Indexed = indexed;
        Faceted = faceted;
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        return ReferenceSchema.InternalBuild(
            Name, Description, DeprecationNotice,
            ReferencedEntityType, ReferencedEntityTypeManaged,
            Cardinality ?? Schemas.Cardinality.ZeroOrMore,
            ReferencedGroupType, ReferencedGroupTypeManaged,
            Indexed, Faceted,
            new Dictionary<string, IAttributeSchema>(),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IReferenceSchema? newReferenceSchema = Mutate(entitySchema!, null);
        IReferenceSchema? existingReferenceSchema = entitySchema!.GetReference(Name);
        if (existingReferenceSchema is null)
        {
            return EntitySchema.InternalBuild(
                entitySchema.Version + 1,
                entitySchema.Name,
                entitySchema.NameVariants,
                entitySchema.Description,
                entitySchema.DeprecationNotice,
                entitySchema.WithGeneratedPrimaryKey,
                entitySchema.WithHierarchy,
                entitySchema.WithPrice,
                entitySchema.IndexedPricePlaces,
                entitySchema.Locales,
                entitySchema.Currencies,
                entitySchema.Attributes,
                entitySchema.AssociatedData,
                entitySchema.References.Values.Concat(new[] {newReferenceSchema}).ToDictionary(x => x.Name, x => x),
                entitySchema.EvolutionModes,
                entitySchema.GetSortableAttributeCompounds()
            );
        }

        if (existingReferenceSchema.Equals(newReferenceSchema))
        {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return entitySchema;
        }

        // ups, there is conflict in associated data settings
        throw new InvalidSchemaMutationException(
            "The reference `" + Name + "` already exists in entity `" + entitySchema.Name + "` schema and" +
            " has different definition. To alter existing reference schema you need to use different mutations."
        );
    }
}