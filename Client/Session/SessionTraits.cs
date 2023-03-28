namespace Client.Session;

public record SessionTraits(string CatalogName, EvitaSessionTerminationCallback? TerminationCallback,
    params SessionFlags[] Flags)
{
    public SessionTraits(string catalogName, params SessionFlags[] flags) : this(catalogName, null, flags)
    {
    }
    public bool IsDryRun() => Flags.Any(x => x == SessionFlags.DryRun);
    public bool IsReadWrite() => Flags.Any(x => x == SessionFlags.ReadWrite);
}

public enum SessionFlags
{
    DryRun,
    ReadWrite
}