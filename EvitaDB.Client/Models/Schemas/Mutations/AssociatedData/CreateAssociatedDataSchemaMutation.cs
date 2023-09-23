using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

public class CreateAssociatedDataSchemaMutation : IAssociatedDataSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public Type Type { get; }
    public bool Localized { get; }
    public bool Nullable { get; }

    public CreateAssociatedDataSchemaMutation(string name, string? description, string? deprecationNotice, Type type,
        bool localized, bool nullable)
    {
        ClassifierUtils.ValidateClassifierFormat(ClassifierType.AssociatedData, name);
        Name = name;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Type = type;
        Localized = localized;
        Nullable = nullable;
    }

    private static IEntitySchemaMutation? MakeMutationIfDifferent<T>(
        IAssociatedDataSchema createdVersion,
        IAssociatedDataSchema existingVersion,
        Func<IAssociatedDataSchema, T> propertyRetriever,
        Func<T, IEntitySchemaMutation> mutationCreator
    )
    {
        T newValue = propertyRetriever.Invoke(createdVersion);
        return Equals(propertyRetriever.Invoke(existingVersion), newValue) ? null : mutationCreator.Invoke(newValue);
    }

    public IAssociatedDataSchema? Mutate(IAssociatedDataSchema? associatedDataSchema)
    {
        return AssociatedDataSchema.InternalBuild(
            Name, Description, DeprecationNotice, Localized, Nullable, Type
        );
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IAssociatedDataSchema? newAssociatedDataSchema = Mutate(null);
        IAssociatedDataSchema? existingAssociatedDataSchema = entitySchema!.GetAssociatedData(Name);
        if (existingAssociatedDataSchema is null)
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
                entitySchema.AssociatedData.Values.Concat(new[] {newAssociatedDataSchema})
                    .ToDictionary(x => x!.Name, x => x)!,
                entitySchema.References,
                entitySchema.EvolutionModes,
                entitySchema.GetSortableAttributeCompounds()
            );
        }

        if (existingAssociatedDataSchema.Equals(newAssociatedDataSchema))
        {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return entitySchema;
        }

        // ups, there is conflict in associated data settings
        throw new InvalidSchemaMutationException(
            "The associated data `" + Name + "` already exists in entity `" + entitySchema.Name + "` schema and" +
            " has different definition. To alter existing associated data schema you need to use different mutations."
        );
    }
}