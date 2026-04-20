using System.Security.Claims;
using System.Text.Json;
using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Dyndns.Service.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Dyndns.Service;

internal static class AppRoutes
{
    private const string AdminPolicy = "AdminOnly";
    private const string AdminRole = "admin";
    private const string AdminUserName = "admin";
    private const string ViewerUserName = "viewer";
    private const string HtmlContentType = "text/html; charset=utf-8";

    public static WebApplication MapAppRoutes(this WebApplication app)
    {
        MapPublicRoutes(app);
        MapAuthRoutes(app);
        MapSessionRoutes(app);
        MapAdminRoutes(app);

        return app;
    }

    private static void MapPublicRoutes(WebApplication app)
    {
        app.MapGet("/", HandleRoot);
        app.MapGet("/dashboard", HandleDashboard);
        app.MapGet("/login", (IWebHostEnvironment environment) => Results.Content(AuthPages.LoginHtml(environment), HtmlContentType));
        app.MapGet("/setup", GetSetupPage);
    }

    private static void MapAuthRoutes(WebApplication app)
    {
        app.MapPost("/api/setup", (Delegate)HandleSetupAsync).AllowAnonymous();
        app.MapPost("/api/login", (Delegate)HandleLoginAsync).AllowAnonymous();
        app.MapPost("/api/logout", (Delegate)HandleLogoutAsync).RequireAuthorization();
    }

    private static void MapSessionRoutes(WebApplication app)
    {
        app.MapGet("/api/me", GetSessionAsync).RequireAuthorization();
        app.MapGet("/api/runs", async (IUpdateCycleRunner runner, CancellationToken cancellationToken)
            => Results.Ok(await runner.ReadRecentRunsAsync(50, cancellationToken))).RequireAuthorization();
        app.MapGet("/api/runs/stream", StreamRunsAsync).RequireAuthorization();
    }

    private static void MapAdminRoutes(WebApplication app)
    {
        app.MapPost("/api/run/check", HandleRunCheckAsync).RequireAuthorization();
        app.MapPost("/api/admin/verify", HandleAdminVerifyAsync).RequireAuthorization(AdminPolicy);
        app.MapPost("/api/run", HandleRunAsync).RequireAuthorization();
        app.MapGet("/api/settings", async (IDynv6SettingsService settingsService, CancellationToken cancellationToken)
            => Results.Ok(await settingsService.GetAsync(cancellationToken))).RequireAuthorization(AdminPolicy);
        app.MapPost("/api/settings", async (
            IDynv6SettingsService settingsService,
            Dynv6Options settings,
            CancellationToken cancellationToken) =>
            {
                try
                {
                    await settingsService.UpdateAsync(settings, cancellationToken);
                    return Results.Ok(await settingsService.GetAsync(cancellationToken));
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(exception.Message);
                }
            }).RequireAuthorization(AdminPolicy);
    }

        private static async Task<IResult> HandleAdminVerifyAsync(
            IUserStore userStore,
            PasswordRequest request,
            CancellationToken cancellationToken)
        {
            var user = await userStore.AuthenticateAsync(AdminUserName, request.Password, cancellationToken);
            if (user is null || !user.IsAdmin)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new { message = "Admin password verified." });
        }

        private static async Task<IResult> HandleRunCheckAsync(
            IUserStore userStore,
            PasswordRequest request,
            CancellationToken cancellationToken)
        {
            var user = await userStore.AuthenticateAsync(ViewerUserName, request.Password, cancellationToken)
                ?? await userStore.AuthenticateAsync(AdminUserName, request.Password, cancellationToken);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new RunAccessResponse(user.IsAdmin ? AdminRole : "viewer"));
        }

        private static async Task<IResult> HandleRunAsync(
            IUserStore userStore,
            RunRequest request,
            IUpdateCycleRunner runner,
            CancellationToken cancellationToken)
        {
            var user = await userStore.AuthenticateAsync(ViewerUserName, request.Password, cancellationToken)
                ?? await userStore.AuthenticateAsync(AdminUserName, request.Password, cancellationToken);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(await runner.RunAsync("manual", cancellationToken, request.ForceUpdate && user.IsAdmin));
        }

    private static async Task StreamRunsAsync(
        HttpContext context,
        IUpdateRunBroadcaster broadcaster,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Response.ContentType = "text/event-stream";

        await context.Response.StartAsync(cancellationToken);

        try
        {
            await foreach (var entry in broadcaster.SubscribeAsync(cancellationToken))
            {
                var payload = JsonSerializer.Serialize(new
                {
                    entry.Id,
                    entry.StartedAt,
                    entry.FinishedAt,
                    entry.Trigger
                });

                await context.Response.WriteAsync("event: run-updated\n", cancellationToken);
                await context.Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException exception)
        {
            // dont log the exception unless debug
            logger.LogDebug(exception, "SSE client connection closed for /api/runs/stream.");
            logger.LogInformation("SSE client connection closed for /api/runs/stream.");
        }
    }

    private static IResult HandleRoot(IUserStore userStore, IWebHostEnvironment environment)
    {
        if (!userStore.IsInitialized)
        {
            return Results.Content(AuthPages.SetupHtml(environment), HtmlContentType);
        }

        return Results.Content(AuthPages.LoginHtml(environment), HtmlContentType);
    }

    private static IResult HandleDashboard(HttpContext context, IUserStore userStore, IWebHostEnvironment environment)
    {
        if (!userStore.IsInitialized)
        {
            return Results.Content(AuthPages.SetupHtml(environment), HtmlContentType);
        }

        return (context.User.Identity?.IsAuthenticated ?? false)
            ? Results.Content(WebUiPage.Html(environment), HtmlContentType)
            : Results.Content(AuthPages.LoginHtml(environment), HtmlContentType);
    }

    private static IResult GetSetupPage(IUserStore userStore, IWebHostEnvironment environment)
        => Results.Content(
            userStore.IsInitialized ? AuthPages.LoginHtml(environment, "The user store is already initialized.") : AuthPages.SetupHtml(environment),
            HtmlContentType);

    private static async Task<IResult> HandleSetupAsync(
        IUserStore userStore,
        SetupRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (userStore.IsInitialized)
        {
            return Results.Conflict("The user store has already been initialized.");
        }

        try
        {
            await userStore.InitializeAsync(request.AdminPassword, request.ViewerPassword, cancellationToken);
            var adminUser = await userStore.AuthenticateAsync(AdminUserName, request.AdminPassword, cancellationToken);
            if (adminUser is not null)
            {
                await SignInAsync(httpContext, adminUser);
            }

            return Results.Ok(new { message = "Users created." });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(exception.Message);
        }
    }

    private static async Task<IResult> HandleLoginAsync(
        IUserStore userStore,
        PasswordRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!userStore.IsInitialized)
        {
            return Results.Conflict("The user store has not been initialized yet.");
        }

        var user = await userStore.AuthenticateAsync(ViewerUserName, request.Password, cancellationToken)
            ?? await userStore.AuthenticateAsync(AdminUserName, request.Password, cancellationToken);
        if (user is null)
        {
            return Results.Json(new { message = "Invalid password." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        await SignInAsync(httpContext, user);
        return Results.Ok(new { message = "Signed in.", dashboardUrl = "/dashboard" });
    }

    private static async Task<IResult> HandleLogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }

    private static IResult GetSessionAsync(HttpContext context)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        var isAdmin = context.User.IsInRole(AdminRole);

        return Results.Ok(new
        {
            isAuthenticated,
            name = isAuthenticated ? context.User.Identity?.Name ?? string.Empty : string.Empty,
            roles = isAdmin ? AdminRole : "none",
            isAdmin
        });
    }

    private static async Task SignInAsync(HttpContext httpContext, AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name)
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, AdminRole));
        }

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            });
    }
}
