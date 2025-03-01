using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MCVIngenieros;
internal class Program
{
    public static void Main(string[] args)
    {
        var appsettings = Helpers.GetConfigurationFileJson();
        string? envPrefix = appsettings["EnvironmentKey"] ?? string.Empty;
        var environmentVariables = Helpers.GetConfigurationEnvironment(envPrefix);
        var config =  Helpers.AggregateConfiguration(
            appsettings,
            environmentVariables
        );
        string
            HostName = 
                config[":hostname"] ?? throw new ArgumentNullException(paramName: ":hostname", Constants.NO_HOST_NAME_AVALIABLE_IN_ENVIRONMENT_VARIABLES),
            HttpToken = 
                config[":httptoken"] ?? throw new ArgumentNullException(paramName: ":httptoken", message: Constants.NO_HTTPTOKEN_NAME_AVALIABLE_IN_ENVIRONMENT_VARIABLES),
            LastPublicIpPath = 
                config["LastPublicIpPath"] ?? Constants.DEFAULT_LAST_IP_PATH,
            DyndnsAPIUrl = 
                config["DyndnsAPIUrl"] ?? Constants.DYNV6_URL,
            DyndnsRecordAPIUrl = 
                config["DyndnsRecordAPIUrl"] ?? Constants.DYNV6_RECORD_URL
            ;
        string[] publicIpProviders = 
                config?.GetSection("PublicIpProviders").Get<string[]>() ?? throw new ArgumentNullException("PublicIpProviders", Constants.NO_IP_PROVIDER_LIST );
        bool ForceUpdate = 
            config.GetValue<bool?>("ForceUpdate") ?? false;
        string lastIp = 
            Helpers.LocateLastIp(LastPublicIpPath);
        IpModel? newIp =
            Helpers.GetNewPublicIp(publicIpProviders);

        // If
        //      ForceUpdate is not set to update
        //      LastIp and NewIp differ
        // Then
        //      Update ZoneIp
        //      Update A Record
        //      Save the new Ip
        //
        if (
            ForceUpdate != false
            || lastIp.Equals(newIp.Ip, StringComparison.OrdinalIgnoreCase) == false
        )
        {
            Dynv6Helper.UpdateDNSZoneIp(HostName!, HttpToken!, LastPublicIpPath, DyndnsAPIUrl, newIp.Ip);
            Dynv6Helper.UpdateDNSZoneARecord(HostName!, HttpToken!, newIp.Ip);
            Helpers.SaveNewIp(LastPublicIpPath, newIp.Ip);
        }
    }
}