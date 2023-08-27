namespace Client.Exceptions;

public class InstanceTerminatedException : EvitaInvalidUsageException
{
    public InstanceTerminatedException(string instanceSpecification) : base($"Evita {instanceSpecification} has been already terminated! No calls are accepted since all resources has been released.")
    {
    }
}