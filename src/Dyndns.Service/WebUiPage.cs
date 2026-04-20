namespace Dyndns.Service;

internal static class WebUiPage
{
    public static string Html(IWebHostEnvironment environment)
        => UiContent.ReadFile(environment, "ui/dashboard.html");
}