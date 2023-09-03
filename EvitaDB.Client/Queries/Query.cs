using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Head;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Queries.Visitor;
using static EvitaDB.Client.Queries.Visitor.PrettyPrintingVisitor;

namespace EvitaDB.Client.Queries;

public class Query
{
    public Collection? Entities { get; }
    public FilterBy? FilterBy { get; }
    public OrderBy? OrderBy { get; }
    public Require? Require { get; }
    
    internal Query(Collection? header, FilterBy? filterBy, OrderBy? orderBy, Require? require) {
        Entities = header;
        FilterBy = filterBy;
        OrderBy = orderBy;
        Require = require;
    }
    
    public string PrettyPrint() => PrettyPrintingVisitor.ToString(this, "\t");
    public override string ToString() => PrettyPrintingVisitor.ToString(this);

    public PrettyPrintingVisitor.StringWithParameters ToStringWithParametersExtraction() =>
        ToStringWithParameterExtraction(this);
}