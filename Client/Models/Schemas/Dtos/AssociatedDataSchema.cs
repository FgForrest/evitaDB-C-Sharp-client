using Client.Utils;

namespace Client.Models.Schemas.Dtos;

public class AssociatedDataSchema
{
    public string Name { get; }
    public IDictionary<NamingConvention, string> NameVariants { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public bool Nullable { get; }
    public bool Localized { get; }
    public Type Type { get; }

    public static AssociatedDataSchema InternalBuild(string name, Type type, bool localized)
    {
        return new AssociatedDataSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            localized, false,
            type
        );
    }

    public static AssociatedDataSchema InternalBuild(string name, bool localized, bool nullable, Type type)
    {
        return new AssociatedDataSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            localized, nullable,
            type
        );
    }

    public static AssociatedDataSchema InternalBuild(string name, string? description, string? deprecationNotice,
        bool localized, bool nullable, Type type)
    {
        return new AssociatedDataSchema(
            name, NamingConventionHelper.Generate(name),
            description, deprecationNotice,
            localized, nullable,
            type
        );
    }

    public static AssociatedDataSchema InternalBuild(string name, IDictionary<NamingConvention, string> nameVariants,
        string? description, string? deprecationNotice, bool localized, bool nullable, Type type)
    {
        return new AssociatedDataSchema(
            name, nameVariants,
            description, deprecationNotice,
            localized, nullable,
            type
        );
    }

    private AssociatedDataSchema(
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        string? deprecationNotice,
        bool localized,
        bool nullable,
        Type type
    )
    {
        Name = name;
        NameVariants = nameVariants;
        Description = description;
        DeprecationNotice = deprecationNotice;
        Localized = localized;
        Nullable = nullable;
        Type = type; //TODO - EvitaDataTypes.toWrappedForm(type)
    }

    public string GetNameVariant(NamingConvention namingConvention) => NameVariants[namingConvention];
    public override string ToString()
    {
        return "AssociatedDataSchema{" +
               "name='" + Name + '\'' +
               ", localized=" + Localized +
               ", nullable=" + Nullable +
               ", type=" + Type +
               '}';
    }
}