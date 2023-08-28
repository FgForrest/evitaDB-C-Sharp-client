namespace Client.Models.Data;

public record DeletedHierarchy<T>(int DeletedEntities, T? DeletedRootEntity);