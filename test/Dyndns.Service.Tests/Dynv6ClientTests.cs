using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Dyndns.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyndns.Service.Tests;

public class Dynv6ClientTests
{
    [Fact]
    public async Task GetZoneByNameAsync_SendsBearerAuthenticatedRequest()
    {
        var handler = new RecordingHttpMessageHandler(
            request => request.RequestUri!.ToString().EndsWith("/zones", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new[]
                    {
                        new Dynv6Zone { Id = 42, Name = "example.com" }
                    })
                }
                : throw new InvalidOperationException($"Unexpected request to {request.RequestUri}"));

        var client = CreateClient(handler);

        var zone = await client.GetZoneByNameAsync("example.com", CancellationToken.None);

        Assert.Equal(42, zone.Id);
        Assert.Equal("example.com", zone.Name);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://dynv6.com/api/zones", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetZoneByNameAsync_ThrowsClearMessageWhenZoneIsMissing()
    {
        var handler = new RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new Dynv6Zone { Id = 1, Name = "other.example.com" }
                })
            });

        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetZoneByNameAsync("example.com", CancellationToken.None));

        Assert.Contains("zone 'example.com' was not found or is not accessible", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateZoneAsync_SendsPatchRequestWithUpdatedIp()
    {
        string? requestBody = null;

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            requestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new Dynv6Zone
                {
                    Id = 42,
                    Name = "example.com",
                    Ipv4Address = "203.0.113.10"
                })
            };
        });

        var client = CreateClient(handler);

        var zone = await client.UpdateZoneAsync(42, new Dynv6ZoneUpdateRequest
        {
            Ipv4Address = "203.0.113.10"
        }, CancellationToken.None);

        Assert.Equal(42, zone.Id);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Equal("https://dynv6.com/api/zones/42", handler.LastRequest.RequestUri!.ToString());
        Assert.NotNull(requestBody);
        Assert.Contains("\"ipv4address\":\"203.0.113.10\"", requestBody);
    }

    [Fact]
    public async Task UpdateRecordAsync_SendsPatchRequestWithUpdatedIp()
    {
        string? requestBody = null;

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            requestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new Dynv6Record
                {
                    Id = 7,
                    ZoneId = 42,
                    Name = "*",
                    Type = "A",
                    Data = "203.0.113.10"
                })
            };
        });

        var client = CreateClient(handler);
        var updateRequest = new Dynv6RecordUpdateRequest
        {
            Name = "*",
            Data = "203.0.113.10",
            Type = "A"
        };

        var record = await client.UpdateRecordAsync(42, 7, updateRequest, CancellationToken.None);

        Assert.Equal(7, record.Id);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Equal("https://dynv6.com/api/zones/42/records/7", handler.LastRequest.RequestUri!.ToString());
        Assert.NotNull(requestBody);
        Assert.Contains("\"data\":\"203.0.113.10\"", requestBody);
    }

    private static Dynv6Client CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var settings = new InMemoryDynv6SettingsService(new Dynv6Options
        {
            DyndnsApiUrl = "https://dynv6.com/api/",
            Key = "test-token"
        });

        return new Dynv6Client(httpClient, NullLogger<Dynv6Client>.Instance, settings);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _sendAsync;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
            : this(request => Task.FromResult(send(request)))
        {
        }

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return await _sendAsync(request);
        }
    }
}