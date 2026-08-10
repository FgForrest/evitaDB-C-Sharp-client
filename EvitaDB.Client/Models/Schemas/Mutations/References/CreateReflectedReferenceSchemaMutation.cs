using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.References;

public class CreateReflectedReferenceSchemaMutation : IReferenceSchemaMutation, ILocalEntitySchemaMutation
{
    public string Name { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public Cardinality? Cardinality { get; }
    public string ReferencedEntityType { get; }
    public string ReflectedReferenceName { get; }
    public bool? Faceted { get; }
    public AttributeInheritanceBehavior AttributeInheritanceBehavior { get; }
    public string[]? AttributeInheritanceFilter { get; }
    public Operation Operation => Operation.Upsert;

    public CreateReflectedReferenceSchemaMutation(
        string name,
        string? description,
        string? deprecationNotice,
        Cardinality? cardinality,
        string referencedEntityType,
        string reflectedReferenceName,
        bool? faceted,
        AttributeInheritanceBehavior attributeInheritanceBehavior,
        string[]? attributeInheritanceFilter
    )
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Reference, name);
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.Entity, referencedEntityType);
        Name = name;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Cardinality = cardinality;
        ReferencedEntityType = referencedEntityType;
        ReflectedReferenceName = reflectedReferenceName;
        Faceted = faceted;
        AttributeInheritanceBehavior = attributeInheritanceBehavior;
        AttributeInheritanceFilter = attributeInheritanceFilter;
    }

    public IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        return ReflectedReferenceSchema.InternalBuild(
            Name, Description, DeprecationNotice,
            ReferencedEntityType, ReflectedReferenceName,
            Cardinality, Faceted,
            new Dictionary<string, IAttributeSchema>(),
            new Dictionary<string, SortableAttributeCompoundSchema>(),
            AttributeInheritanceBehavior,
            AttributeInheritanceFilter
        );
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        ReflectedReferenceSchema newReferenceSchema = (ReflectedReferenceSchema) Mutate(entitySchema!, null)!;
        IReferenceSchema? referencedReferenceSchema = catalogSchema
            .GetEntitySchema(newReferenceSchema.ReferencedEntityType)
            ?.GetReference(newReferenceSchema.ReflectedReferenceName);
        IReferenceSchema referenceToInsert = referencedReferenceSchema is not null
            ? newReferenceSchema.WithReferencedSchema(referencedReferenceSchema)
            : newReferenceSchema;
        IReferenceSchema? existingReferenceSchema = entitySchema!.GetReference(Name);
        if (existingReferenceSchema is null)
        {
            return EntitySchema.InternalBuild(
                entitySchema.Version + 1,
                entitySchema.Name,
                entitySchema.NameVariants,
                entitySchema.Description,
                entitySchema.DeprecationNotice,
                entitySchema.WithGeneratedPrimaryKey(),
                entitySchema.WithHierarchy(),
                entitySchema.WithPrice(),
                entitySchema.IndexedPricePlaces,
                entitySchema.Locales,
                entitySchema.Currencies,
                entitySchema.Attributes,
                entitySchema.AssociatedData,
                entitySchema.References.Values.Concat(new[] {referenceToInsert}).ToDictionary(x => x.Name, x => x),
                entitySchema.EvolutionModes,
                entitySchema.GetSortableAttributeCompounds()
            );
        }

        if (existingReferenceSchema.Equals(newReferenceSchema))
        {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return entitySchema;
        }

        throw new InvalidSchemaMutationException(
            $"The reference `{Name}` already exists in entity `{entitySchema.Name}` schema and" +
            " has different definition. To alter existing reference schema you need to use different mutations."
        );
    }
}
