namespace EvitaDB.Client.Session;

public enum CatalogState
{
    WarmingUp,
    Alive,
    UnknownCatalogState,
    Corrupted,
    Inactive,
    GoingAlive,
    BeingActivated,
    BeingDeactivated,
    BeingCreated,
    BeingDeleted,
    Missing,
    OutOfDate,
    BeingUpgraded
}