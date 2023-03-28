using System.Collections.Concurrent;

namespace Client.Pooling;

public class ChannelPool
{
    private readonly ConcurrentQueue<ChannelInvoker> _channels = new();
    private readonly ChannelBuilder _channelBuilder;

    public ChannelPool(ChannelBuilder channelBuilder, int poolSize)
    {
        _channelBuilder = channelBuilder;
        for (int i = 0; i < poolSize; i++)
        {
            _channels.Enqueue(channelBuilder.Build());
        }
    }
    
    public ChannelInvoker GetChannel()
    {
        return _channels.TryDequeue(out var channel) ? channel : _channelBuilder.Build();
    }
    
    public void ReturnChannel(ChannelInvoker channel)
    {
        //TODO: fix this
        /*if (channel.Channel.State is ConnectivityState.Shutdown or ConnectivityState.TransientFailure)
            return;*/
        _channels.Enqueue(channel);
    }
    
    public async void Shutdown()
    {
        while (_channels.TryDequeue(out var channel))
        {
            await channel.Channel.ShutdownAsync();
        }
    }
}