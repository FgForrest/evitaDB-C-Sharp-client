using Grpc.Core;
using Grpc.Net.Client;

namespace EvitaDB.Client.Pooling;

public class ChannelInvoker
{
    public GrpcChannel Channel { get; }
    public CallInvoker Invoker { get; }

    public ChannelInvoker(GrpcChannel channel, CallInvoker invoker)
    {
        Channel = channel;
        Invoker = invoker;
    }
}