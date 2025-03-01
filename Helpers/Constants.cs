using System.Text.Json;

namespace MCVIngenieros;

public static class Constants 
{
    public const string DEFAULT_LAST_IP_PATH = ".\\lasIp.txt"
        , DYNV6_URL = "https://ipv4.dynv6.com/api/update?ipv4=$ipv4&zone=$hostname&token=$httpToken"
        , DYNV6_RECORD_URL = "https://dynv6.com/api/v2/zones/$zoneID/records/$recordID"
        , NO_IP_PROVIDER_LIST = "PublicIpProviders Array must be filled with http urls to Public IP APIs in order to check if ip changed."
        , NO_HOST_NAME_AVALIABLE_IN_ENVIRONMENT_VARIABLES = "No {prefix}__hostname key was found in environment variables."
        , NO_HTTPTOKEN_NAME_AVALIABLE_IN_ENVIRONMENT_VARIABLES = "No {prefix}__httptoken key was found in environment variables."
        ;

    public readonly static
        JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
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
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
            SslProtocols = System.Security.Authentication.SslProtocols.None
        };

    public readonly static
        HttpClient httpClient = new(httpClientHandler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
}
