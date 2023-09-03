namespace EvitaDB.Client.Exceptions;

public interface IEvitaError
{
    string ErrorCode { get; }
    string PrivateMessage { get; }
    string PublicMessage { get; }
}