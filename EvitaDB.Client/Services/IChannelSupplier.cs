using Client.Pooling;

namespace Client.Services;

public interface IChannelSupplier
{
    ChannelInvoker GetChannel();
    void ReleaseChannel();
}