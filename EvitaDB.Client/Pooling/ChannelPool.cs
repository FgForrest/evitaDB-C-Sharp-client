using System.Collections.Concurrent;

namespace EvitaDB.Client.Pooling;

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
    
    public void ReleaseChannel(ChannelInvoker channel)
    {
        //TODO: fix this
        /*if (channel.Channel.State is ConnectivityState.Shutdown or ConnectivityState.TransientFailure)
            return;*/
        _channels.Enqueue(channel);
    }
    
    public bool Shutdown()
    {
        IList<Task> tasks = new List<Task>();
        while (_channels.TryDequeue(out ChannelInvoker? channel))
        {
            tasks.Add(channel.Channel.ShutdownAsync());
        }
        Task.WhenAll(tasks);
        return _channels.IsEmpty;
    }
}