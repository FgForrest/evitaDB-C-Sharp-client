namespace Client.DataTypes.Data;

public interface IDataItem
{
    void Accept(IDataItemVisitor visitor);
    bool Empty { get; }
}