namespace Client.Utils;

public class AtomicReference<T> where T : class
{
    private T? _atomicValue;

    public AtomicReference(T originalValue)
    {
        _atomicValue = originalValue;
    }

    public AtomicReference()
    {
        _atomicValue = default;
    }
    
    public T? Value
    {
        get => Volatile.Read(ref _atomicValue);
        set => Volatile.Write(ref _atomicValue, value);
    }
    
    public bool CompareAndSet(T expected, T newValue)
    {
        var previous = Interlocked.CompareExchange(ref _atomicValue, newValue, expected);
        return ReferenceEquals(previous, expected);
    }
    
    public T? GetAndSet(T newValue)
    {
        return Interlocked.Exchange(ref _atomicValue, newValue);
    }

    public T? GetAndSet(Func<T?, T?> updateFunction)
    {
        T? oldValue, newValue;
        do
        {
            oldValue = _atomicValue;
            newValue = updateFunction(oldValue);
        } while (Interlocked.CompareExchange(ref _atomicValue, newValue, oldValue) != oldValue);
        return oldValue;
    }
}