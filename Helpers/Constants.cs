using System.Text.Json;

namespace Helpers;

public static class Constants 
{
    public readonly static
        JsonSerializerOptions JsonSerializerOptions = new()
        {
            AllowTrailingCommas = true,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
        };

    public readonly static
        HttpClientHandler httpClientHandler = new()
        {
            AllowAutoRedirect = true,
            CheckCertificateRevocationList = false,
            ClientCertificateOptions = ClientCertificateOption.Manual,
            Credentials = null,
            PreAuthenticate = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            SslProtocols = System.Security.Authentication.SslProtocols.None,
            MaxConnectionsPerServer = int.MaxValue,
            MaxResponseHeadersLength = int.MaxValue,
            MaxAutomaticRedirections = int.MaxValue,
            MaxRequestContentBufferSize = int.MaxValue,
        };

    public static HttpClient GetHttpClient() => new(httpClientHandler)
    {
        Timeout = TimeSpan.FromSeconds(30),
    };
}
