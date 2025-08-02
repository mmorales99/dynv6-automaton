using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DnsClient;

public class DnsClientFactory (IConfiguration config, IServiceProvider services)
{
    private static readonly Dictionary<string, IDnsClient> _clients = [];

    private static string Normalize(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        return str.Trim().ToUpperInvariant();
    }

    private static Type? LoadFromDll(string path)
    {
        var asm = Assembly.LoadFrom(path);
        var type = asm.GetTypes().FirstOrDefault(t => typeof(IDnsClient).IsAssignableFrom(t));
        return type;
    }

    private static Type? CompileFromSource(string path)
    {
        string code = File.ReadAllText(path);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        string dllName = Path.GetFileName(path);

        var compilation = CSharpCompilation.Create(dllName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IDnsClient).Assembly.Location)
            )
            .AddSyntaxTrees(syntaxTree);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
            throw new Exception("Error al compilar el cliente DNS.");

        ms.Seek(0, SeekOrigin.Begin);
        var asm = Assembly.Load(ms.ToArray());
        var type = asm.GetTypes().FirstOrDefault(t => typeof(IDnsClient).IsAssignableFrom(t));
        return type;
    }

    private static Type? GetInternalClient(string name)
    {
        var type = Type.GetType(name)
            ?? (AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass =>
                ass.GetName().Name!.Equals("DnsClient", StringComparison.OrdinalIgnoreCase)) ?? throw new BadImageFormatException("No se encuentra la dll DnsClient."))
            .ExportedTypes.FirstOrDefault(t =>
                (t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(name + "client", StringComparison.OrdinalIgnoreCase))
                && typeof(IDnsClient).IsAssignableFrom(t)
            ) ?? throw new TypeLoadException($"Cliente integrado '{name}' no encontrado."); ;
        return type;
    }

    private static Type GetClientType(string name) 
    {
        Type? type;
        if (File.Exists(name))
        {
            string ext = Path.GetExtension(name).ToLowerInvariant();

            type = ext switch
            {
                ".dll" => LoadFromDll(name),
                ".cs" => CompileFromSource(name),
                _ => throw new NotSupportedException("Extensión de cliente no soportada.")
            };
        }
        else
        {
            type = GetInternalClient(name);
        }

        if (type == null)
            throw new NullReferenceException($"No se han encontrado clientes con nombre {name}.");

        return type;
    }

    private IDnsClient BuildInstance(Type type)
    {
        // Buscar constructor compatible
        var ctor = type.GetConstructors()
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1 &&
                       (parameters[0].ParameterType == typeof(IConfiguration) ||
                        (parameters[0].ParameterType.IsGenericType &&
                         parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IOptions<>)));
            }) 
            ?? throw new InvalidOperationException($"No se ha encontrado un constructor válido para {type.Name}.");

        var paramType = ctor.GetParameters()[0].ParameterType;

        object? arg = null;

        if (paramType == typeof(IConfiguration))
        {
            arg = config;
        }
        else if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(IOptions<>))
        {
            var optionsType = paramType.GenericTypeArguments[0];
            var instance = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException($"No se pudo crear una instancia de {optionsType.Name}");
            
            // Intentar bindear desde ambas secciones
            var sectionNames = new[]
            {
                optionsType.Name,
                optionsType.Name.EndsWith("Options")
                    ? optionsType.Name[..^"Options".Length]
                    : optionsType.Name
            };

            foreach (var sectionName in sectionNames)
            {
                var section = config.GetSection(sectionName);
                if (section.Exists())
                {
                    section.Bind(instance);
                }
            }

            var wrapperType = typeof(OptionsWrapper<>).MakeGenericType(optionsType);
            arg = Activator.CreateInstance(wrapperType, instance);
        }

        return (IDnsClient)(
            ctor.Invoke([arg])
            ?? throw new InvalidOperationException($"No se ha podido crear una nueva instancia del cliente {type.Name}.")
        );
    }

    private IDnsClient? GetInstance(string name) 
    {
        if (string.IsNullOrEmpty(Normalize(name))) 
        {
            return null;
        }

        if (_clients.TryGetValue(Normalize(name), out IDnsClient? value))
        {
            return value;
        }

        Type? type = GetClientType(name);
        var client = BuildInstance(type);
        _clients[name] = client;
        return client;
    }


    public void UpdateDNSRecord(string dnsClient, bool updateRecord, string ip)
    {
        if (updateRecord) 
        {
            var client = GetInstance(dnsClient);
            if (!string.IsNullOrEmpty(ip))
                client?.UpdateARecords(ip);
        }
    }
}
