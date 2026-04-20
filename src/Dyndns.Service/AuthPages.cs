namespace Dyndns.Service;

internal static class AuthPages
{
    public static string LoginHtml(IWebHostEnvironment environment, string? errorMessage = null)
        => RenderPage(environment, "Sign in", "Access the Dynv6 dashboard with the viewer or admin password.", "ui/login-body.html", errorMessage);

    public static string SetupHtml(IWebHostEnvironment environment, string? errorMessage = null)
        => RenderPage(environment, "First setup", "Create the two local users. The resulting BSON file is stored read-only after it is created.", "ui/setup-body.html", errorMessage);

    private static string RenderPage(IWebHostEnvironment environment, string title, string subtitle, string bodyTemplatePath, string? errorMessage)
    {
        var body = UiContent.RenderTemplate(environment, bodyTemplatePath, ("{{ERROR_MESSAGE}}", Escape(errorMessage)));
        return UiContent.RenderTemplate(
            environment,
            "ui/auth.html",
            ("{{TITLE}}", Escape(title)),
            ("{{SUBTITLE}}", Escape(subtitle)),
            ("{{BODY}}", body));
    }

    private static string Escape(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
}