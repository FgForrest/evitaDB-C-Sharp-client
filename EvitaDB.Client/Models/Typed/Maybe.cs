namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Distinguishes the three states a typed-contract member can be in, which a plain nullable
/// reference cannot express:
/// <list type="bullet">
/// <item><description><see cref="NotFetched"/> - the data was not requested by the query, so nothing
/// is known about it. Reading it in Java would raise <c>ContextMissingException</c>.</description></item>
/// <item><description><see cref="Absent"/> - the data was fetched and the entity genuinely has no
/// value for it.</description></item>
/// <item><description><see cref="Present"/> - the data was fetched and a value exists.</description></item>
/// </list>
/// <remarks>
/// <see cref="NotFetched"/> is deliberately the zero value so that <c>default(Maybe&lt;T&gt;)</c> is
/// honest rather than claiming an absent value was fetched.
/// </remarks>
/// </summary>
public enum MaybeState : byte
{
    /// <summary>The data was not fetched, so its existence is unknown.</summary>
    NotFetched = 0,

    /// <summary>The data was fetched and no value exists.</summary>
    Absent = 1,

    /// <summary>The data was fetched and a value exists.</summary>
    Present = 2
}

/// <summary>
/// A three-state optional used by typed entity contracts. Members declared as
/// <c>Maybe&lt;string&gt; Name { get; }</c> can tell "not fetched" apart from "fetched but empty",
/// which the convenience shape <c>string? Name { get; }</c> collapses into <c>null</c>.
/// <remarks>
/// <para>
/// Ported from the Java driver's use of <c>Optional</c> plus a declared
/// <c>throws ContextMissingException</c> to signal availability. C# has neither construct, so the
/// distinction moves into this type.
/// </para>
/// <para>
/// Divergence from the Java driver worth knowing: an <b>empty collection is reported as
/// <see cref="MaybeState.Present"/></b> with an empty value, whereas Java normalises empty
/// collections to <c>Optional.empty()</c>. Reporting an empty list as "absent" reads as a bug in C#.
/// </para>
/// </remarks>
/// </summary>
/// <typeparam name="T">type of the carried value</typeparam>
public readonly struct Maybe<T> : IEquatable<Maybe<T>>
{
    private readonly T? _value;

    private Maybe(MaybeState state, T? value)
    {
        State = state;
        _value = value;
    }

    /// <summary>Availability and presence of the value.</summary>
    public MaybeState State { get; }

    /// <summary>True when the underlying data was fetched, regardless of whether a value exists.</summary>
    public bool IsFetched => State != MaybeState.NotFetched;

    /// <summary>True when the data was fetched and a value exists.</summary>
    public bool HasValue => State == MaybeState.Present;

    /// <summary>
    /// The carried value, or throws when there is none. Prefer <see cref="TryGetValue"/> or
    /// <see cref="ValueOrDefault"/> when absence is expected.
    /// </summary>
    /// <exception cref="InvalidOperationException">when the value is not present</exception>
    public T Value => HasValue
        ? _value!
        : throw new InvalidOperationException(
            State == MaybeState.NotFetched
                ? $"Value of type `{typeof(T)}` was not fetched - widen the query requirements to fetch it."
                : $"Value of type `{typeof(T)}` was fetched but is not present."
        );

    /// <summary>The carried value, or the default of <typeparamref name="T"/> when there is none.</summary>
    public T? ValueOrDefault => HasValue ? _value : default;

    /// <summary>Returns the carried value or <paramref name="fallback"/> when there is none.</summary>
    public T ValueOr(T fallback) => HasValue ? _value! : fallback;

    /// <summary>Retrieves the carried value without throwing.</summary>
    /// <param name="value">receives the value when present</param>
    /// <returns>true when a value is present</returns>
    public bool TryGetValue(out T value)
    {
        value = HasValue ? _value! : default!;
        return HasValue;
    }

    /// <summary>Represents data that was not fetched by the query.</summary>
    public static Maybe<T> NotFetched => default;

    /// <summary>Represents data that was fetched but holds no value.</summary>
    public static Maybe<T> Absent => new(MaybeState.Absent, default);

    /// <summary>Represents a fetched, existing value.</summary>
    /// <param name="value">the value; <c>null</c> collapses to <see cref="Absent"/></param>
    public static Maybe<T> Of(T? value) =>
        value is null ? Absent : new Maybe<T>(MaybeState.Present, value);

    /// <summary>Wraps an existing value.</summary>
    public static implicit operator Maybe<T>(T? value) => Of(value);

    /// <inheritdoc />
    public bool Equals(Maybe<T> other) =>
        State == other.State && EqualityComparer<T?>.Default.Equals(_value, other._value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine((byte)State, _value);

    /// <inheritdoc />
    public override string ToString() => State switch
    {
        MaybeState.Present => $"Present({_value})",
        MaybeState.Absent => "Absent",
        _ => "NotFetched"
    };

    /// <summary>Compares two instances for equality.</summary>
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>Compares two instances for inequality.</summary>
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);
}
