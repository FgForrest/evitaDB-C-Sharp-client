namespace EvitaDB.Client.Queries;

public interface IConstraintWithSuffix
{
    string? SuffixIfApplied { get; }

    bool ArgumentImplicitForSuffix(object argument);
}