using System.Text;
using EvitaDB.Client.DataTypes.Data;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.DataTypes;

public record ComplexDataObject
{
    public IDataItem Root { get; }

    public ComplexDataObject(IDataItem root)
    {
        Assert.IsTrue(root is not DataItemValue, "Root item cannot be a value item!");
        Root = root;
    }

    public bool Empty => Root.Empty && Root is not DataItemArray;

    public void Accept(IDataItemVisitor visitor) => Root.Accept(visitor);

    public override int GetHashCode()
    {
        return Root.GetHashCode();
    }

    public override string ToString()
    {
        ToStringDataItemVisitor visitor = new ToStringDataItemVisitor(3);
        Accept(visitor);
        return visitor.GetAsString();
    }

    public virtual bool Equals(ComplexDataObject? other)
    {
        if (this == other) return true;
        if (other is null || GetType() != other.GetType()) return false;
        return Equals(Root, other.Root);
    }
}

internal class ToStringDataItemVisitor : IDataItemVisitor
{
    private StringBuilder AsString { get; } = new();
    private int Indentation { get; }
    private int _current;

    public ToStringDataItemVisitor(int indentation = 0)
    {
        Indentation = indentation;
    }

    public string GetAsString()
    {
        return AsString.ToString();
    }

    public void Visit(DataItemArray arrayItem)
    {
        if (arrayItem.Empty)
        {
            AsString.Append("[]");
        }
        else
        {
            AsString.Append("[\n");
            _current += Indentation;
            arrayItem.ForEach((dataItem, hasNext) =>
            {
                AsString.Append(new string(' ', _current));
                if (dataItem == null)
                {
                    AsString.Append("<NULL>");
                }
                else
                {
                    dataItem.Accept(this);
                }

                if (hasNext)
                {
                    AsString.Append(',');
                }

                AsString.Append('\n');
            });
            _current -= Indentation;
            AsString.Append(new string(' ', _current)).Append(']');
        }
    }

    public void Visit(DataItemMap mapItem)
    {
        if (mapItem.Empty)
        {
            AsString.Append("{}");
        }
        else
        {
            AsString.Append("{\n");
            _current += Indentation;
            mapItem.ForEachSorted((propertyName, dataItem, hasNext) =>
            {
                AsString.Append(new string(' ', _current)).Append(EvitaDataTypes.FormatValue(propertyName))
                    .Append(": ");
                if (dataItem == null)
                {
                    AsString.Append("<NULL>");
                }
                else
                {
                    dataItem.Accept(this);
                }

                if (hasNext)
                {
                    AsString.Append(',');
                }

                AsString.Append('\n');
            });
            _current -= Indentation;
            AsString.Append(new string(' ', _current)).Append('}');
        }
    }

    public void Visit(DataItemValue valueItem)
    {
        object? value = valueItem.Value;
        AsString.Append(value == null ? "<NULL>" : EvitaDataTypes.FormatValue(value));
    }
}