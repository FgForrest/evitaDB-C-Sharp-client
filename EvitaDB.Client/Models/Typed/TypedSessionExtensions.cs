using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Session-level entry points for typed entity contracts: deriving a schema from a contract, and
/// reading query results as contract instances.
/// <remarks>
/// <para>
/// These are deliberately distinct method names rather than overloads of <c>QueryOne</c> /
/// <c>QueryList</c>. Those are constrained to <see cref="IEntityClassifier"/>, and relying on a
/// constraint mismatch to fall through to an extension method produces confusing diagnostics and would
/// break the moment the constraint is relaxed.
/// </para>
/// <para>
/// Schema definition is synchronous because the driver's underlying schema-update API is; reads follow
/// the driver convention of an async core with a synchronous facade.
/// </para>
/// </remarks>
/// </summary>
public static class TypedSessionExtensions
{
    /// <summary>
    /// Derives an entity schema from an annotated contract type and applies it to the catalog.
    /// </summary>
    /// <typeparam name="T">a contract annotated with <see cref="EvitaEntityAttribute"/></typeparam>
    /// <param name="session">an open read-write session</param>
    /// <returns>the stored schema, as fetched back from the server</returns>
    public static ISealedEntitySchema DefineEntitySchemaFromModelType<T>(this EvitaClientSession session) =>
        session.DefineEntitySchemaFromModelType(typeof(T));

    /// <summary>
    /// Derives an entity schema from an annotated contract type and applies it to the catalog.
    /// </summary>
    /// <param name="session">an open read-write session</param>
    /// <param name="contractType">a contract annotated with <see cref="EvitaEntityAttribute"/></param>
    /// <returns>the stored schema, as fetched back from the server</returns>
    public static ISealedEntitySchema DefineEntitySchemaFromModelType(
        this EvitaClientSession session,
        Type contractType)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(contractType);

        var descriptor = TypedContractAnalyzer.Describe(contractType);
        var builder = session.DefineEntitySchema(descriptor.EntityType);
        ModelSchemaAnalyzer.Apply(descriptor, builder);
        return session.UpdateAndFetchEntitySchema(builder);
    }

    /// <summary>
    /// Returns the evitaDB entity type a contract maps to.
    /// </summary>
    /// <typeparam name="T">an annotated contract</typeparam>
    public static string GetEntityTypeOfModel<T>() => TypedContractAnalyzer.Describe<T>().EntityType;

    /// <summary>
    /// Executes a query and returns at most one result as a typed contract instance.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract</typeparam>
    /// <param name="session">an open session</param>
    /// <param name="query">the query to execute; must fetch entity bodies</param>
    /// <param name="cancellationToken">token propagated to the call</param>
    public static async Task<T?> QueryOneModelAsync<T>(
        this EvitaClientSession session,
        Query query,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(session);

        var entity = await session.QueryOneSealedEntityAsync(query, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : TypedEntityFactory.Create<T>(entity);
    }

    /// <summary>
    /// Synchronous facade over <see cref="QueryOneModelAsync{T}"/>.
    /// </summary>
    public static T? QueryOneModel<T>(this EvitaClientSession session, Query query) where T : class =>
        session.QueryOneModelAsync<T>(query).GetAwaiter().GetResult();

    /// <summary>
    /// Executes a query and returns all results as typed contract instances.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract</typeparam>
    /// <param name="session">an open session</param>
    /// <param name="query">the query to execute; must fetch entity bodies</param>
    /// <param name="cancellationToken">token propagated to the call</param>
    public static async Task<IList<T>> QueryModelListAsync<T>(
        this EvitaClientSession session,
        Query query,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(session);

        var entities = await session.QueryListOfSealedEntitiesAsync(query, cancellationToken)
            .ConfigureAwait(false);
        return entities.Select(TypedEntityFactory.Create<T>).ToList();
    }

    /// <summary>
    /// Synchronous facade over <see cref="QueryModelListAsync{T}"/>.
    /// </summary>
    public static IList<T> QueryModelList<T>(this EvitaClientSession session, Query query) where T : class =>
        session.QueryModelListAsync<T>(query).GetAwaiter().GetResult();

    /// <summary>
    /// Fetches a single entity by primary key and returns it as a typed contract instance. The entity
    /// collection is taken from the contract, so no collection needs to be named at the call site.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract</typeparam>
    /// <param name="session">an open session</param>
    /// <param name="primaryKey">primary key of the entity</param>
    /// <param name="require">content requirements controlling what is fetched</param>
    /// <param name="cancellationToken">token propagated to the call</param>
    public static async Task<T?> GetModelAsync<T>(
        this EvitaClientSession session,
        int primaryKey,
        IEntityContentRequire[] require,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(session);

        var entityType = TypedContractAnalyzer.Describe<T>().EntityType;
        var entity = await session
            .GetEntityAsync(entityType, primaryKey, require, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : TypedEntityFactory.Create<T>(entity);
    }

    /// <summary>
    /// Synchronous facade over <see cref="GetModelAsync{T}"/>.
    /// </summary>
    public static T? GetModel<T>(
        this EvitaClientSession session,
        int primaryKey,
        params IEntityContentRequire[] require) where T : class =>
        session.GetModelAsync<T>(primaryKey, require).GetAwaiter().GetResult();

    /// <summary>
    /// Creates a typed editor over a brand-new entity of the contract's collection, taking the schema from
    /// the session.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract, normally extending <see cref="ITypedEntityEditor"/></typeparam>
    /// <param name="session">an open session</param>
    /// <param name="primaryKey">
    /// primary key to assign, or <c>null</c> to let the server generate one
    /// </param>
    public static T CreateNewModel<T>(this EvitaClientSession session, int? primaryKey = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(session);

        var entityType = TypedContractAnalyzer.Describe<T>().EntityType;
        var schema = session.GetEntitySchemaOrThrow(entityType);
        return TypedEntityFactory.CreateNew<T>(schema, primaryKey);
    }

    /// <summary>
    /// Opens an already-fetched entity for writing through an editor contract. The fetched entity is not
    /// mutated - changes accumulate in a builder.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract, normally extending <see cref="ITypedEntityEditor"/></typeparam>
    /// <param name="entity">the fetched entity to base the changes on</param>
    public static T OpenForWrite<T>(this ISealedEntity entity) where T : class =>
        TypedEntityFactory.OpenForWrite<T>(entity);

    /// <summary>
    /// Sends the mutations accumulated by a typed editor through the session.
    /// </summary>
    /// <param name="session">an open read-write session</param>
    /// <param name="editor">an instance created by <see cref="CreateNewModel{T}"/> or <see cref="OpenForWrite{T}"/></param>
    /// <returns>reference to the upserted entity</returns>
    public static EntityReference UpsertModel(this EvitaClientSession session, ITypedEntityEditor editor)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(editor);

        return editor.UpsertVia(session);
    }

    /// <summary>
    /// Reads an already-fetched entity as a typed contract instance.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract</typeparam>
    /// <param name="entity">the fetched entity</param>
    /// <exception cref="EvitaInvalidUsageException">when the contract does not match the entity type</exception>
    public static T AsModel<T>(this ISealedEntity entity) where T : class =>
        TypedEntityFactory.Create<T>(entity);
}
