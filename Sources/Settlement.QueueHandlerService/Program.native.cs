using Fanex.Caching.Redis;
using Fanex.Data;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.MailDeliver.MailClient;
using Fanex.RemotingRef;
using Settlement.Consumer.Models;
using Settlement.QueueHandlerService;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddMassTransitWithRabbitMQ(builder.Configuration);
services.AddFanexData(builder.Configuration);

services.Configure<AppSettingsKey>(builder.Configuration);
services.AddHttpClient();
services.AddCoreServices();
services.AddFanexLogSentry(builder.Configuration);
services.AddAutoMapper();
services.AddRedisCache(configuration.GetSection("fanex.caching.redis"));
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