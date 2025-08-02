namespace Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Cargar appsettings.json
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        // Cargar variables de entorno
        builder.Configuration.AddEnvironmentVariables();
        ConfigureClients(builder);

        builder.Services.AddDnsClientFactory();
        builder.Services.AddHelpers();
        builder.Services.AddHostedService<src.Worker>();

        var host = builder.Build();
        host.Run();
    }

    private static void ConfigureClients(HostApplicationBuilder builder)
    {
        var supportedExtensions = new[] { ".json", ".ini", ".xml" };
        // Cargar archivos de configuración de la carpeta Configs/*
        var configPath = Path.Combine(AppContext.BaseDirectory, "Configs");
        if (Directory.Exists(configPath))
        {
            var configFiles = Directory.GetFiles(configPath)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var file in configFiles)
            {
                var ext = Path.GetExtension(file).ToLower();
                switch (ext)
                {
                    case ".json":
                        builder.Configuration.AddJsonFile(file, optional: false, reloadOnChange: true);
                        break;
                    case ".ini":
                        builder.Configuration.AddIniFile(file, optional: false, reloadOnChange: true);
                        break;
                    case ".xml":
                        builder.Configuration.AddXmlFile(file, optional: false, reloadOnChange: true);
                        break;
                }
            }
        }
    }
}