using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class AssociatedDataSchema : IAssociatedDataSchema
{
    public string Name { get; }
    public IDictionary<NamingConvention, string?> NameVariants { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public bool Nullable { get; }
    public bool Localized { get; }
    public Type Type { get; }

    internal static AssociatedDataSchema InternalBuild(string name, Type type, bool localized = false)
    {
        return new AssociatedDataSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            localized, false,
            type
        );
    }

    internal static AssociatedDataSchema InternalBuild(string name, bool localized, bool nullable, Type type)
    {
        return new AssociatedDataSchema(
            name, NamingConventionHelper.Generate(name),
            null, null,
            localized, nullable,
            type
        );
    }

    internal static AssociatedDataSchema InternalBuild(string name, string? description, string? deprecationNotice,
        bool localized, bool nullable, Type type)
    {
        return new AssociatedDataSchema(
            name, NamingConventionHelper.Generate(name),
            description, deprecationNotice,
            localized, nullable,
            type
        );
    }

    internal static AssociatedDataSchema InternalBuild(string name, IDictionary<NamingConvention, string?> nameVariants,
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
        IDictionary<NamingConvention, string?> nameVariants,
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
        Type = EvitaDataTypes.IsSupportedTypeOrItsArray(type) ? type : typeof(ComplexDataObject);
    }

    public string? GetNameVariant(NamingConvention namingConvention) => NameVariants.TryGetValue(namingConvention, out string? name) ? name : null;
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