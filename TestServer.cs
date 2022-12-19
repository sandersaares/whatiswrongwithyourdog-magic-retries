using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed class TestServer : IAsyncDisposable
{
    /// <summary>
    /// The port that the server is listening on, on the loopback interface.
    /// Value is available after Start() is called.
    /// </summary>
    public ushort PortNumber { get; private set; }

    public int RequestCount;

    public TestServer()
    {
        var address = "http://127.0.0.1:0";

        _host = WebHost.CreateDefaultBuilder()
            .UseUrls(address)
            .Configure(app =>
            {
                var logger = app.ApplicationServices.GetRequiredService<ILogger<TestServer>>();

                void AddHandler(string path, Action<HttpContext> handler)
                {
                    app.Map(path, x => x.Use((HttpContext context, RequestDelegate next) =>
                    {
                        Interlocked.Increment(ref RequestCount);
                        logger.LogInformation($"Handling request for {path}. Total requests handled: {RequestCount}");

                        handler(context);
                        return Task.CompletedTask;
                    }));
                }

                AddHandler("/disconnect", context =>
                {
                    context.Abort();
                });
            })
        .Build();
    }

    public string CreateUrl(string path)
    {
        return $"http://127.0.0.1:{PortNumber}/{path.TrimStart('/')}";
    }

    private readonly IWebHost _host;

    public void Start()
    {
        _host.Start();

        var serverAddresses = _host.ServerFeatures.Get<IServerAddressesFeature>();
        PortNumber = (ushort)new Uri(serverAddresses.Addresses.Single()).Port;
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
