namespace Client.Exceptions;

public interface IEvitaError
{
    string PrivateMessage { get; }
    string PublicMessage { get; }
}