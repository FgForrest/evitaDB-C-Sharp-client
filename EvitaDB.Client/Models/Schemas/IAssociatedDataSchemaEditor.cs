using System.Globalization;

namespace EvitaDB.Client.Models.Schemas;

public interface IAssociatedDataSchemaEditor : IAssociatedDataSchema, INamedSchemaWithDeprecationEditor<IAssociatedDataSchemaEditor>
{
    /// <summary>
    /// Localized associated data has to be ALWAYS used in connection with specific <see cref="CultureInfo"/>. In other
    /// words - it cannot be stored unless associated locale is also provided.
    /// </summary>
    /// <returns>builder to continue with configuration</returns>
    new IAssociatedDataSchemaEditor Localized();
    
    /// <summary>
    /// Localized associated data has to be ALWAYS used in connection with specific <see cref="CultureInfo"/>. In other
    /// words - it cannot be stored unless associated locale is also provided.
    /// </summary>
    /// <param name="decider">decider returns true when attribute should be localized</param>
    /// <returns>builder to continue with configuration</returns>
    new IAssociatedDataSchemaEditor Localized(Func<bool> decider);
    
    /// <summary>
    /// When attribute is nullable, its values may be missing in the entities. Otherwise, the system will enforce
    /// non-null checks upon upserting of the entity.
    /// </summary>
    /// <returns>builder to continue with configuration</returns>
    new IAssociatedDataSchemaEditor Nullable();
    
    /// <summary>
    /// When attribute is nullable, its values may be missing in the entities. Otherwise, the system will enforce
    /// non-null checks upon upserting of the entity.
    /// </summary>
    /// <param name="decider">decider returns true when attribute should be localized</param>
    /// <returns>builder to continue with configuration</returns>
    new IAssociatedDataSchemaEditor Nullable(Func<bool> decider);
}
