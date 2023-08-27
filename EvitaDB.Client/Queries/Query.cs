using Client.Queries.Filter;
using Client.Queries.Head;
using Client.Queries.Order;
using Client.Queries.Requires;
using Client.Queries.Visitor;

using static Client.Queries.Visitor.PrettyPrintingVisitor;

namespace Client.Queries;

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

    public StringWithParameters ToStringWithParametersExtraction() =>
        ToStringWithParameterExtraction(this);
}