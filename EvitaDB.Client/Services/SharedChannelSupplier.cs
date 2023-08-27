using Client.Pooling;

namespace Client.Services;

public class SharedChannelSupplier : IChannelSupplier
{
    private readonly ChannelInvoker _channel;

    public SharedChannelSupplier(ChannelInvoker channel)
    {
        _channel = channel;
    }

    public ChannelInvoker GetChannel()
    {
        return _channel;
    }

    public void ReleaseChannel()
    {
        // do nothing, its shared channel
    }
}