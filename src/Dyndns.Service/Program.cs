using Dyndns.Service;
using Dyndns.Service.Options;
using Dyndns.Service.Services;

var hostBuilder = Host.CreateDefaultBuilder(args)
	.UseWindowsService(options =>
	{
		options.ServiceName = "Dynv6 Automaton";
	})
	.ConfigureServices((context, services) =>
	{
		services.Configure<Dynv6Options>(context.Configuration.GetSection(Dynv6Options.SectionName));
		services.Configure<SmtpNotificationOptions>(context.Configuration.GetSection(SmtpNotificationOptions.SectionName));
		services.PostConfigure<Dynv6Options>(ApplyEnvironmentOverrides);
		services.AddHttpClient<IpifyClient>();
		services.AddSingleton<IPublicIpProvider, IpifyPublicIpProvider>();
		services.AddSingleton<IIpChangeChecker, IpChangeChecker>();
		services.AddSingleton<IIpChangeManager, IpChangeManager>();
		services.AddSingleton<IUpdateFailurePolicy, UpdateFailurePolicy>();
		services.AddSingleton<IUpdateFailureNotifier, SmtpUpdateFailureNotifier>();
		services.AddHttpClient<IDynv6Client, Dynv6Client>();
		services.AddSingleton<IUpdateStateStore, FileUpdateStateStore>();
		services.AddSingleton<IDnsUpdateService, DnsUpdateService>();
		services.AddHostedService<DnsUpdateWorker>();
	});

var host = hostBuilder.Build();

await host.RunAsync();

static void ApplyEnvironmentOverrides(Dynv6Options options)
{
	if (string.IsNullOrWhiteSpace(options.EnvironmentKey))
	{
		return;
	}

	options.ZoneName = Environment.GetEnvironmentVariable($"{options.EnvironmentKey}__ZONE_NAME") ?? options.ZoneName;
	options.Key = Environment.GetEnvironmentVariable($"{options.EnvironmentKey}__KEY") ?? options.Key;
}
