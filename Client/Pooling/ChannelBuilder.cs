using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace Client.Pooling;

public class ChannelBuilder
{
    public string Host { get; }
    public int Port { get; }
    public Interceptor[] Interceptors { get; }
    public GrpcChannelOptions Options { get; }

    public ChannelBuilder(string host, int port, HttpMessageHandler httpClientHandler, params Interceptor[] interceptors)
    {
        Host = host;
        Port = port;
        Options = new GrpcChannelOptions { HttpClient = new HttpClient(httpClientHandler)};
        Interceptors = interceptors;
    }

    public ChannelInvoker Build()
    {
        var channel = GrpcChannel.ForAddress($"https://{Host}:{Port}", Options);
        return new ChannelInvoker(channel, channel.Intercept(Interceptors));
    }
}