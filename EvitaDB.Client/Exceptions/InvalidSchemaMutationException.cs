using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Exceptions;

public class InvalidSchemaMutationException : SchemaAlteringException
{
    public InvalidSchemaMutationException(string message) : base(message)
    {
    }

    public InvalidSchemaMutationException(string entityType, CatalogEvolutionMode necessaryEvolutionMode) : 
        this("The entity collection `" + entityType + "` doesn't exist and would be automatically created," +
        " providing that catalog schema allows `" + necessaryEvolutionMode + "`" +
        " evolution mode.")
    {
        
    }
}
