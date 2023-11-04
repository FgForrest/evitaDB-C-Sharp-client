namespace EvitaDB.Client.Queries;

public interface IConstraintContainerWithSuffix : IConstraintWithSuffix
{
    bool ChildImplicitForSuffix(IConstraint? child) {
        return false;
    }
    
    bool AdditionalChildImplicitForSuffix(IConstraint? child) {
        return false;
    }
}