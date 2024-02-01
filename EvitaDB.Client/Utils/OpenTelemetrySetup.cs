using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EvitaDB.Client.Utils;

public class OpenTelemetrySetup : IDisposable
{
    private const string ServiceName = "EvitaDB C# driver";

    private readonly TracerProvider? _tracerProvider;

    public OpenTelemetrySetup(string traceEndpointUrl, string? protocol)
    {
        OtlpExportProtocol otlpExportProtocol = Enum.TryParse(protocol, out OtlpExportProtocol parsedProtocol)
            ? parsedProtocol
            : OtlpExportProtocol.Grpc;
        _tracerProvider = Sdk
            .CreateTracerProviderBuilder()
            .AddSource(ServiceName)
            .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
            .AddGrpcClientInstrumentation(opt => opt.SuppressDownstreamInstrumentation = true)
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(traceEndpointUrl);
                options.Protocol = otlpExportProtocol;
            })
            .Build();
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
    }
}
