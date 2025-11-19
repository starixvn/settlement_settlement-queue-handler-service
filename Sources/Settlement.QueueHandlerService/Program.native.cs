using Fanex.Data.SqlServer;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.MailDeliver.MailClient;
using Fanex.RemotingRef;
using Fanex.SiteDbConnectionManager.Client;
using ONEbook.UM.DependencyInjection;
using Settlement.JackTime;
using Settlement.QueueHandlerService;
using Settlement.QueueHandlerService.Services;
using Settlement.Shared;
using Settlement.Shared.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddMassTransitWithRabbitMQ(builder.Configuration);
services.AddFanexSql(builder.Configuration);

services.Configure<AppSettingsKey>(builder.Configuration);
services.AddHttpClient();
services.AddCoreServices();
services.AddJackTimeServices();
services.AddSharedServices();
services.AddFanexLogSentry(builder.Configuration);
services.AddAutoMapper();
services.AddRedisCacheConfig(builder.Configuration);
services.AddOneBookUmForService(builder.Configuration);
services.AddHostedService<AutoJackTimeService>();
services.AddHostedService<GetNotificationService>();
services.AddFanexLogSentry(configuration);
services.AddSiteDbConnectionManager();

var remotingRefUrl = configuration.GetSection("Fanex.RemotingRef.Service.Url").Value;
if (!string.IsNullOrEmpty(remotingRefUrl))
{
    RefDataTypeContainer
        .ConnectTo(new Uri(remotingRefUrl))
        .FromApplication("Settlement");
}

AppSettings.AppMailId = configuration.GetSection("MailServer:Id").Value;
AppSettings.AppMailAddress = configuration.GetSection("MailServer:Address").Value;

services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/healthz");

await app.RunAsync();