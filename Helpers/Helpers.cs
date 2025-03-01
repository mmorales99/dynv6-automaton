using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MCVIngenieros;

public static class Helpers
{
    // +------------+
    // |    TODOs   |
    // +------------+
    //
    // + Maybe add support for cmd args as config?


    /// <summary>
    /// Aggregates IConfiguration objects.
    /// </summary>
    /// <param name="configurations">IConfiguration object list with readed configurations.</param>
    /// <returns>Single IConfiguration implementation from Microsoft.Extensions.Configuration that aggregates all given configurations.</returns>
    public static IConfiguration AggregateConfiguration(params IConfiguration[] configurations) 
    {
        var builder = new ConfigurationBuilder();
        foreach (var config in configurations)
        {
            builder.AddConfiguration(config);
        }
        return builder.Build();
    }

    /// <summary>
    /// Returns a IConfiguration object from given path and file name.
    /// </summary>
    /// <param name="configPath">Base path for config DIRECTORY. Defaults to current directory.</param>
    /// <param name="configFile">Config File Name. Defaults to appsettings.json</param>
    /// <param name="optional">Marks if config file could be ommited or not present. Defaults to false.</param>
    /// <param name="reloadOnChange">Marks if config file its monitorized and reload config object when file changes. Defaults to true when debugging and false either way.</param>
    /// <returns>IConfiguration implementation from Microsoft.Extensions.Configuration. Contains the keys present in json file given as config file.</returns>
    public static IConfiguration GetConfigurationFileJson(
        string configPath = "",
        string configFile = "appsettings.json",
        bool optional = false,
        bool? reloadOnChange = null
    ) 
    {
        reloadOnChange ??= Debugger.IsAttached;

        var path = string.IsNullOrEmpty(configPath) 
            ? Directory.GetCurrentDirectory() 
            : configPath;

        var builder = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile(
                path: configFile, 
                optional: optional, 
                reloadOnChange: (bool)reloadOnChange
            );

        return builder.Build();
    }

    /// <summary>
    /// Returns a IConfiguration object from given path and file name.
    /// </summary>
    /// <param name="configPath">Base path for config DIRECTORY. Defaults to current directory.</param>
    /// <param name="configFile">Config File Name. Defaults to appconfig.xml</param>
    /// <param name="optional">Marks if config file could be ommited or not present. Defaults to false.</param>
    /// <param name="reloadOnChange">Marks if config file its monitorized and reload config object when file changes. Defaults to true when debugging and false either way.</param>
    /// <returns>IConfiguration implementation from Microsoft.Extensions.Configuration. Contains the keys present in XML file given as config file.</returns>
    public static IConfiguration GetConfigurationFileXML(
        string configPath = "",
        string configFile = "appconfig.xml",
        bool optional = false,
        bool? reloadOnChange = null
    )
    {
        reloadOnChange ??= Debugger.IsAttached;

        var path = string.IsNullOrEmpty(configPath)
            ? Directory.GetCurrentDirectory()
            : configPath;

        var builder = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddXmlFile(
                path: configFile,
                optional: optional,
                reloadOnChange: (bool)reloadOnChange
            );

        return builder.Build();
    }

    /// <summary>
    /// Returns a IConfiguration object containing environment variables.
    /// </summary>
    /// <param name="environmentPrefix">Prefix that variables must start with. If ignored, no prefix is used.</param>
    /// <returns>IConfiguration implementation from Microsoft.Extensions.Configuration. Contains the keys present in machine and user environment variables.</returns>
    public static IConfiguration GetConfigurationEnvironment(
        string environmentPrefix = ""
    )
    {
        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables(environmentPrefix);

        return builder.Build();
    }

    public static IConfiguration GetConfigurationSecrets(
        string userSecretsId = "",
        bool reloadOnChange = true
    )
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets(
                userSecretsId: userSecretsId,
                reloadOnChange: reloadOnChange
            );

        return builder.Build();
    }

    /// <summary>
    /// Given a list of public ip api providers, tryes to get a fresh public ip number.
    /// <remarks>APIs must return a JSON containing the pair "ip":"A.N.Y.IP".</remarks>
    /// </summary>
    /// <param name="publicIpProviders">List of http uris that will be used for public ip check.</param>
    /// <returns>Return a model Ip that will be used</returns>
    /// <exception cref="ArgumentNullException">When no</exception>
    public static IpModel GetNewPublicIp(params string[] publicIpProviders)
    {
        if (publicIpProviders == null)
        {
            throw new ArgumentNullException(paramName: nameof(publicIpProviders), message: Constants.NO_IP_PROVIDER_LIST);
        }

        IpModel? newIp = null;
        foreach (var ipProvider in publicIpProviders)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(ipProvider),
                };
                var response = Constants.httpClient.Send(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadFromJsonAsync<IpModel>(
                        options: Constants.JsonSerializerOptions
                    ).Result;

                    if (content == null)
                    {
                        continue;
                    }

                    newIp = content;
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        if (newIp == null)
        {
            throw new NullReferenceException(
                "The Public Ip Providers did not give a valid information. Try new providers list or wait until they are back online. Check your connection as last resort."
                );
        }

        return newIp;
    }

    /// <summary>
    /// Given the usual location of LastIp, overwrites the file with the new Ip.
    /// </summary>
    /// <param name="LastPublicIpPath">Path to the file containing the last ip used.</param>
    /// <param name="newIp">The new ip, proved to be working.</param>
    public static void SaveNewIp(
        string LastPublicIpPath,
        string newIp
    ) 
    {
        File.WriteAllText(
                LastPublicIpPath,
                newIp
            );
    }

    /// <summary>
    /// Tryes to locate the lastip.txt file where last used ip was.
    /// </summary>
    /// <param name="LastPublicIpPath">Path the containing file.</param>
    /// <returns>Last ip used for dyndns.</returns>
    public static string LocateLastIp(string LastPublicIpPath)
    {
        string lastIp = string.Empty;

        if (File.Exists(LastPublicIpPath))
        {
            string[] lines = File.ReadAllLines(
                LastPublicIpPath,
                encoding: Encoding.UTF8
            );

            lines = [..
                lines.Where(
                    l => string.IsNullOrEmpty(l) == false
                )
            ];

            if (lines == null || lines.Length == 0)
            {
                lastIp = string.Empty;
            }
            else
            {
                lastIp = lines[0];
            }
        }
        else
        {
            File.WriteAllText(LastPublicIpPath, "");
        }

        return lastIp;
    }
}
