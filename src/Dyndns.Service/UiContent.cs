namespace Dyndns.Service;

internal static class UiContent
{
    public static string ReadFile(IWebHostEnvironment environment, string relativePath)
    {
        var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        return File.ReadAllText(Path.Combine(webRootPath, relativePath));
    }

    public static string RenderTemplate(IWebHostEnvironment environment, string relativePath, params (string token, string value)[] replacements)
    {
        var content = ReadFile(environment, relativePath);

        foreach (var (token, value) in replacements)
        {
            content = content.Replace(token, value, StringComparison.Ordinal);
        }

        return content;
    }
}