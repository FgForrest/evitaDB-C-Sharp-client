using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// A nested editor attached to one of its parent's references, to be upserted together with it.
/// </summary>
/// <param name="ReferenceName">name of the reference on the parent entity</param>
/// <param name="Editor">the editor holding the referenced entity's changes</param>
public readonly record struct TypedReferenceAttachment(string ReferenceName, ITypedEntityEditor Editor);

/// <summary>
/// Upserts an editor together with everything attached to it.
/// <remarks>
/// <para>
/// Order matters: a parent cannot reference a child that does not exist yet, and when the server assigns
/// the child's primary key the parent must learn it. So children are upserted first, depth-first, and each
/// returned primary key is written into the parent's reference before the parent itself is sent - the same
/// leaf-first ordering and key backfill the Java driver performs in <c>upsertEntityDeeply</c>.
/// </para>
/// <para>
/// The upsert itself is injected rather than taken from a session, which keeps the ordering and backfill
/// logic testable without a server.
/// </para>
/// </remarks>
/// </summary>
public static class TypedDeepUpsert
{
    /// <summary>
    /// Upserts every attached editor leaf-first, backfilling generated primary keys into the referencing
    /// entity, and finally upserts the root.
    /// </summary>
    /// <param name="root">the editor to upsert together with its attachments</param>
    /// <param name="upsert">sends one builder and returns the reference to the upserted entity</param>
    /// <returns>references to everything upserted, children before their parents, root last</returns>
    /// <exception cref="EvitaInvalidUsageException">
    /// when the attachments form a cycle, or a child comes back without a primary key
    /// </exception>
    public static IReadOnlyList<EntityReference> Upsert(
        ITypedEntityEditor root,
        Func<IEntityBuilder, EntityReference> upsert)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(upsert);

        List<EntityReference> upserted = [];
        UpsertRecursively(root, upsert, upserted, new HashSet<ITypedEntityEditor>(Identity.Instance),
            new Dictionary<ITypedEntityEditor, int?>(Identity.Instance));
        return upserted;
    }

    /// <summary>
    /// Upserts one editor after everything it depends on, and returns its primary key - the one the server
    /// assigned when the entity was new.
    /// </summary>
    private static int? UpsertRecursively(
        ITypedEntityEditor editor,
        Func<IEntityBuilder, EntityReference> upsert,
        List<EntityReference> upserted,
        HashSet<ITypedEntityEditor> onPath,
        Dictionary<ITypedEntityEditor, int?> resolved)
    {
        // an editor attached to two parents is upserted once and its key reused
        if (resolved.TryGetValue(editor, out var already))
        {
            return already;
        }

        if (!onPath.Add(editor))
        {
            throw new EvitaInvalidUsageException(
                "Attached editors form a cycle, so there is no order in which they can be upserted - " +
                "detach one side and set its reference by primary key instead."
            );
        }

        foreach (var attachment in editor.Attachments)
        {
            var childKey = UpsertRecursively(attachment.Editor, upsert, upserted, onPath, resolved)
                           ?? throw new EvitaInvalidUsageException(
                               $"Entity upserted for reference `{attachment.ReferenceName}` came back " +
                               $"without a primary key, so the reference cannot be established."
                           );

            BoundTypedContract.EstablishReference(editor.EntityBuilder, attachment.ReferenceName, childKey);
        }

        onPath.Remove(editor);

        var key = editor.EntityBuilder.ToInstance().PrimaryKey;

        // a new entity is always sent - creating it is the change, whether or not it carries any data.
        // An existing one is sent only when something was actually written to it
        if (editor.EntityBuilder is InitialEntityBuilder
            || TypedEntityView.Normalize(editor.ToMutation()) is not null)
        {
            var reference = upsert(editor.EntityBuilder);
            upserted.Add(reference);

            // a new entity has no key until the server assigns one, and it arrives in the reference
            key = reference.PrimaryKey ?? key;
        }

        resolved[editor] = key;
        return key;
    }

    /// <summary>
    /// Compares editors by reference, so that two editors over equal entities still count as distinct.
    /// </summary>
    private sealed class Identity : IEqualityComparer<ITypedEntityEditor>
    {
        internal static readonly Identity Instance = new();

        public bool Equals(ITypedEntityEditor? x, ITypedEntityEditor? y) => ReferenceEquals(x, y);

        public int GetHashCode(ITypedEntityEditor obj) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
