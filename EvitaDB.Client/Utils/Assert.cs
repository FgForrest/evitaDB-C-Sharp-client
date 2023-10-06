using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Utils;

internal static class Assert
{
    public static void IsTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new EvitaInvalidUsageException(message);
        }
    }
    
    public static void IsTrue<T>(bool condition, Func<T> exceptionFactory) where T : Exception
    {
        if (!condition)
        {
            throw exceptionFactory.Invoke();
        }
    }

    public static void NotNull(object? obj, string message)
    {
        if (obj == null)
        {
            throw new EvitaInvalidUsageException(message);
        }
    }
    
    public static void NotNull<T>(object? obj, Func<T> exceptionFactory) where T : Exception
    {
        if (obj is null)
        {
            throw exceptionFactory.Invoke();
        }
    }
    
    public static void IsPremiseValid(object? obj, string message)
    {
        if (obj == null)
        {
            throw new EvitaInternalError(message);
        }
    }
    
    public static void IsPremiseValid<T>(object? obj, Func<T> exceptionFactory) where T : Exception
    {
        if (obj is null)
        {
            throw exceptionFactory.Invoke();
        }
    }
}