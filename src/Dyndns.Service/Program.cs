using Dyndns.Service;
using Dyndns.Service.Options;
using Dyndns.Service.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
	options.ServiceName = "Dynv6 Automaton";
});

builder.WebHost.UseUrls(builder.Configuration["WebUi:Url"] ?? "http://localhost:5050");

builder.Services.Configure<Dynv6Options>(builder.Configuration.GetSection(Dynv6Options.SectionName));
builder.Services.Configure<SmtpNotificationOptions>(builder.Configuration.GetSection(SmtpNotificationOptions.SectionName));
builder.Services.PostConfigure<Dynv6Options>(ApplyEnvironmentOverrides);
builder.Services.AddHttpClient<IpifyClient>();
builder.Services.AddSingleton<IPublicIpProvider, IpifyPublicIpProvider>();
builder.Services.AddSingleton<IIpChangeChecker, IpChangeChecker>();
builder.Services.AddSingleton<IIpChangeManager, IpChangeManager>();
builder.Services.AddSingleton<IUpdateFailurePolicy, UpdateFailurePolicy>();
builder.Services.AddSingleton<IUpdateFailureNotifier, SmtpUpdateFailureNotifier>();
builder.Services.AddHttpClient<IDynv6Client, Dynv6Client>();
builder.Services.AddSingleton<IDynv6SettingsService, FileDynv6SettingsService>();
builder.Services.AddSingleton<IUpdateStateStore, FileUpdateStateStore>();
builder.Services.AddSingleton<IUpdateRunHistoryStore, FileUpdateRunHistoryStore>();
builder.Services.AddSingleton<IDnsUpdateService, DnsUpdateService>();
builder.Services.AddSingleton<IUpdateCycleRunner, UpdateCycleRunner>();
builder.Services.AddSingleton<IUserStore, FileBsonUserStore>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.Cookie.Name = "Dynv6.Automaton.Auth";
		options.Cookie.HttpOnly = true;
		options.Cookie.SameSite = SameSiteMode.Lax;
		options.ExpireTimeSpan = TimeSpan.FromDays(7);
		options.SlidingExpiration = true;
	});
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});
builder.Services.AddHostedService<DnsUpdateWorker>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAppRoutes();

await app.RunAsync();

const string EnvironmentPrefix = "DYNV6_UPDATER";

static void ApplyEnvironmentOverrides(Dynv6Options options)
{
	options.ZoneName = Environment.GetEnvironmentVariable($"{EnvironmentPrefix}__ZONE_NAME") ?? options.ZoneName;
	options.Key = Environment.GetEnvironmentVariable($"{EnvironmentPrefix}__KEY") ?? options.Key;
}
