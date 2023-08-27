namespace Client.DataTypes.Data;

public interface IDataItemVisitor
{
    void Visit(DataItemArray dataItemArray);
    void Visit(DataItemMap dataItemMap);
    void Visit(DataItemValue dataItemValue);
}