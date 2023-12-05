namespace EvitaDB.Client.Models.ExtraResults;

public interface IPrettyPrintable
{
    /// <summary>
    /// Returns pretty-printed string representation of the object in a text (MarkDown format).
    /// </summary>
    /// <returns>pretty-printed string representation of the object</returns>
    string PrettyPrint();
}
