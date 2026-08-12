namespace EvitaDB.Client.DataTypes;

/// <summary>
/// Represents an evitaDB expression in its minimal string form (e.g. <c>$pageNumber % 2 == 0</c>). The expression
/// language is evaluated exclusively on the server side - the client only carries the expression string over
/// the wire.
/// </summary>
public sealed record Expression(string MinimalForm)
{
    public override string ToString() => MinimalForm;
}
