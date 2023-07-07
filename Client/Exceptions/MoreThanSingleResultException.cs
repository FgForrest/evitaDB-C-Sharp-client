namespace Client.Exceptions;

public class MoreThanSingleResultException : ArgumentException
{
    public MoreThanSingleResultException(string message) : base(message)
    {
    }
}