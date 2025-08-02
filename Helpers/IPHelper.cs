using Helpers.Options;
using Microsoft.Extensions.Options;

namespace Helpers;

public class IPHelper(IOptions<IpInfoOptions> options)
{
    public bool CheckPublicIP(out string? ip)
    {
        ip = null;
        // Leer de configuración la lista de proveedores de IPs públicas
        var providers = options.Value.PublicIpProviders;
        string? newIp = GetPublicIp(providers);
        
        if (string.IsNullOrWhiteSpace(newIp))
            return false; // No se pudo obtener IP

        ip = newIp;

        // Verificar si la IP guardada en .\.lastIp es diferente a la nueva IP obtenida
        var lastIpPath = Path.Join(Environment.CurrentDirectory, @".data", options.Value.LastPublicIpPath);
        string? lastIp = null;
        if (File.Exists(lastIpPath))
        {
            lastIp = File.ReadAllText(lastIpPath).Trim();
        }

        if (lastIp != newIp)
        {
            var parentDir = Path.GetDirectoryName(lastIpPath);
            Directory.CreateDirectory(parentDir);
            File.WriteAllText(lastIpPath, newIp);
            return true;
        }
        return false;
    }

    private static string? GetPublicIp(List<string> providers)
    {
        string? newIp = null;
        // Atacar a todos los proveedores en paralelo y quedarse con la primera respuesta válida
        var tasks = providers.Select(async url => {
            try
            {
                using var httpClient = Helpers.Constants.GetHttpClient();
                var response = await httpClient.GetStringAsync(url);
                // Extraer la IP del JSON o texto plano
                var ip = ExtractIpFromResponse(response);
                return ip;
            }
            catch
            {
                return null;
            }
        }).ToList();

        while (tasks.Count > 0)
        {
            var finished = Task.WhenAny(tasks).Result;
            newIp = finished.Result;
            if (!string.IsNullOrWhiteSpace(newIp))
                break;
            tasks.Remove(finished);
        }

        return newIp;
    }

    // Método auxiliar para extraer la IP de la respuesta (JSON o texto plano)
    private static string? ExtractIpFromResponse(string response)
    {
        string? ip = null;
        // Intentar extraer IP de JSON
        try
        {
            List<string> ipFieldNames = ["ip", "ip_addr", "ip_address"]; // TODO: hacer que se amplie con los del config de cada proveedor
            // Buscar campos comunes en JSON
            var json = System.Text.Json.JsonDocument.Parse(response);
            foreach (string ipFieldName in ipFieldNames)
            {
                if (json.RootElement.TryGetProperty(ipFieldName, out var jsonElement))
                {
                    ip = jsonElement.GetString();
                }
            }
        }
        catch
        {
            // Si no es JSON, intentar devolver el texto plano
            var trimmed = response.Trim();
            if (System.Net.IPAddress.TryParse(trimmed, out _))
                return trimmed;
        }
        return ip;
    }
}