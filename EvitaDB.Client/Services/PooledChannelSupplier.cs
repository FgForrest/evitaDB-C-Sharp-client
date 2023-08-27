using Client.Pooling;
using Client.Utils;

namespace Client.Services;

public class PooledChannelSupplier : IChannelSupplier
{
    private readonly ChannelPool _channelPool;
    private ChannelInvoker? Channel { get; set; }
    
    public PooledChannelSupplier(ChannelPool channelPool)
    {
        _channelPool = channelPool;
    }
    
    public ChannelInvoker GetChannel()
    {
        return Channel ??= _channelPool.GetChannel();
    }

    public void ReleaseChannel()
    {
        Assert.IsPremiseValid(
            Channel is not null,
            "Channel instance is not leased from pool. Do you call the `releaseChannel()` correctly?"
        );
        _channelPool.ReleaseChannel(Channel!);
    }
}