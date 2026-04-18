using System.Net;
using System.Net.Http.Json;
using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Dyndns.Service.Services;

namespace Dyndns.Service.Tests;

public class IpifyClientTests
{
    [Fact]
    public async Task GetCurrentIpv4AddressAsync_ReturnsParsedIpv4Address()
    {
        var handler = new IpifyRecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new PublicIpResponse { Ip = "203.0.113.42" })
        });

        var client = CreateClient(handler);

        var ipAddress = await client.GetCurrentIpv4AddressAsync(CancellationToken.None);

        Assert.Equal("203.0.113.42", ipAddress);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://api.ipify.org/?format=json", handler.LastRequest.RequestUri!.ToString());
    }

    private static IpifyClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Microsoft.Extensions.Options.Options.Create(new Dynv6Options
        {
            PublicIpUrl = "https://api.ipify.org?format=json"
        });

        return new IpifyClient(httpClient, options);
    }

    private sealed class IpifyRecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _send;

        public IpifyRecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
        {
            _send = send;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_send(request));
        }
    }
}