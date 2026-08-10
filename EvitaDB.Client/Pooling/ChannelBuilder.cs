using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace EvitaDB.Client.Pooling;

public class ChannelBuilder
{
    public string Host { get; }
    public int Port { get; }
    public Interceptor[] Interceptors { get; }
    public GrpcChannelOptions Options { get; }
    public bool UseTls { get; }

    public ChannelBuilder(string host, int port, bool useTls, HttpMessageHandler httpClientHandler, params Interceptor[] interceptors)
    {
        Host = host;
        Port = port;
        Options = new GrpcChannelOptions { HttpClient = new HttpClient(httpClientHandler)};
        Interceptors = interceptors;
        UseTls = useTls;
    }

    public ChannelInvoker Build()
    {
        var protocol = UseTls ? "https" : "http";
        var channel = GrpcChannel.ForAddress($"{protocol}://{Host}:{Port}", Options);
        return new ChannelInvoker(channel, channel.Intercept(Interceptors));
    }
}
