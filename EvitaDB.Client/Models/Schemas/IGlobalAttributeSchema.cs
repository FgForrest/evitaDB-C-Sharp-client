using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// This schema is an extension of standard <see cref="AttributeSchema"/> that adds support for marking the attribute as
/// globally unique. The global attribute schema can be used only at <see cref="CatalogSchema"/> level.
/// 
/// </summary>
public interface IGlobalAttributeSchema : IEntityAttributeSchema
{
    /// <summary>
    /// When attribute is unique globally it is automatically filterable, and it is ensured there is exactly one single
    /// entity having certain value of this attribute in entire <see cref="ICatalog"/>.
    /// <see cref="IAttributeSchema.Type"/> of the unique attribute must be one of comparable types.
    /// 
    /// As an example of unique attribute can be URL - there is no sense in having two entities with same URL, and it's
    /// better to have this ensured by the database engine.
    /// </summary>
    bool UniqueGlobally { get; }
    
    /// <summary>
    /// When attribute is unique globally it is automatically filterable, and it is ensured there is exactly one single
    /// entity having certain value of this attribute in entire <see cref="ICatalog"/>.
    /// <see cref="IAttributeSchema.Type"/> of the unique attribute must be one of comparable types.
    /// 
    /// As an example of unique attribute can be URL - there is no sense in having two entities with same URL, and it's
    /// better to have this ensured by the database engine.
    /// 
    /// This property differs from <see cref="UniqueGlobally"/> in that it is possible to have multiple entities with same
    /// value of this attribute as long as the attribute is <see cref="IAttributeSchema.Localized"/> and the values relate to different
    /// locales.
    /// </summary>
    bool UniqueGloballyWithinLocale { get; }
    
    /// <summary>
    /// Returns type of uniqueness of the attribute. See <see cref="UniqueGlobally"/> and <see cref="UniqueGloballyWithinLocale"/>.
    /// </summary>
    GlobalAttributeUniquenessType GlobalUniquenessType { get; }
}
