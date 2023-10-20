namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// <remarks>
/// <para>
/// Interface that simply combines <see cref="IEntityAttributeSchemaEditor"/> and <see cref="IGlobalAttributeSchema"/>
/// entity contracts together. Builder produces either <see cref="IEntitySchemaMutation"/> that describes all changes
/// to be made on <see cref="IEntitySchema"/> instance to get it to "up-to-date" state or can provide already built
/// </para>
/// <para>
/// <see cref="IEntitySchema"/> that may not represent globally "up-to-date" state because it is based on
/// the version of the entity known when builder was created.
/// </para>
/// <para>
/// Mutation allows Evita to perform surgical updates on the latest version of the <see cref="IEntitySchema"/>
/// object that is in the database at the time update request arrives.
/// </para>
/// </remarks>
/// </summary>
public interface IEntityAttributeSchemaBuilder : IEntityAttributeSchemaEditor<IEntityAttributeSchemaBuilder>
{
}