using System.Net;
using System.Net.Http.Headers;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace EvitaDB.Test.Utils;

public class CustomWaitStrategy : IWaitUntil
{
    public async Task<bool> UntilAsync(IContainer container)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        var httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        while (true)
        {
            try
            {
                var message =
                    new HttpRequestMessage(HttpMethod.Get,
                        $"http://{container.Hostname}:{container.GetMappedPublicPort(5555)}/system/server-name")
                    {
                        Version = new Version(2, 0),
                        Headers =
                        {
                            Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json") },
                            UserAgent = { ProductInfoHeaderValue.Parse("EvitaDB.Test") }
                        }
                    };
                await httpClient.SendAsync(message);
                return true;
            }
            catch (Exception)
            {
                // wait
            }
        }
    }
}
