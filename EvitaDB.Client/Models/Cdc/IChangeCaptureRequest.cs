namespace EvitaDB.Client.Models.Cdc;

public interface IChangeCaptureRequest
{
    CaptureContent Content { get; }
}
