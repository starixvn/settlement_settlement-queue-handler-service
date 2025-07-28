using Fanex.Caching.Redis;
using Fanex.Data.SqlServer;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.MailDeliver.MailClient;
using Fanex.RemotingRef;
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
services.AddRedisCache(configuration.GetSection("fanex.caching.redis"));
services.AddHostedService<AutoJackTimeService>();
services.AddOneBookUmForService(builder.Configuration);
services.AddFanexLogSentry(configuration);
RefDataTypeContainer
    .ConnectTo(new Uri(configuration.GetSection("Fanex.RemotingRef.Service.Url").Value))
    .FromApplication("Settlement");

AppSettings.AppMailId = configuration.GetSection("MailServer:Id").Value;
AppSettings.AppMailAddress = configuration.GetSection("MailServer:Address").Value;



var app = builder.Build();


app.Run(async context =>
{
    await context.Response.WriteAsync("OK");
});
await app.RunAsync();