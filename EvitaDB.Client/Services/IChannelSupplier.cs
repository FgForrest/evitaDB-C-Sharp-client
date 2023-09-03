using EvitaDB.Client.Pooling;

namespace EvitaDB.Client.Services;

public interface IChannelSupplier
{
    ChannelInvoker GetChannel();
    void ReleaseChannel();
}