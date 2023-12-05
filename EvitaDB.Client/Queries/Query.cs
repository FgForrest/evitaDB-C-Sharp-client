using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Head;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Queries.Visitor;
using static EvitaDB.Client.Queries.Visitor.PrettyPrintingVisitor;

namespace EvitaDB.Client.Queries;

/// <summary>
/// Main transfer object for Evita Query Language. Contains all data and conditions that query what entities will
/// be queried, in what order and how rich the returned results will be.
/// evitaDB query language is composed of nested set of functions. Each function has its name and set of arguments inside
/// round brackets. Arguments and functions are delimited by a comma. Strings are enveloped inside apostrophes. This language
/// is expected to be used by human operators, on the code level query is represented by a query object tree, that can
/// be constructed directly without intermediate string language form. For the sake of documentation human readable form
/// is used here.
/// Query has these four parts:
/// <list type="bullter">
///     <item><term><see cref="Collection"/>: contains collection (mandatory) specification</term></item>
///     <item><term><see cref="FilterBy"/>: contains constraints limiting entities being returned (optional, if missing all are returned)</term></item>
///     <item><term><see cref="OrderBy"/>: defines in what order will the entities return (optional, if missing entities are ordered by primary integer key in ascending order)</term></item>
///     <item><term><see cref="Require"/>: contains additional information for the query engine, may hold pagination settings, richness of the entities and so on (optional, if missing only primary keys of all the entities are returned)</term></item>
/// </list>
/// </summary>
public class Query
{
    public Collection? Collection { get; }
    public FilterBy? FilterBy { get; }
    public OrderBy? OrderBy { get; }
    public Require? Require { get; }
    
    internal Query(Collection? header, FilterBy? filterBy, OrderBy? orderBy, Require? require) {
        Collection = header;
        FilterBy = filterBy;
        OrderBy = orderBy;
        Require = require;
    }
    
    public string PrettyPrint() => PrettyPrintingVisitor.ToString(this, "\t");
    public override string ToString() => PrettyPrintingVisitor.ToString(this);

    public StringWithParameters ToStringWithParametersExtraction() =>
        ToStringWithParameterExtraction(this);
}
