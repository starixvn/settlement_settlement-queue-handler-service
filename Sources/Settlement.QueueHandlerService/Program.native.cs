using Fanex.Data;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.MailDeliver.MailClient;
using Fanex.RemotingRef;
using Settlement.Consumer.Models;
using Settlement.QueueHandlerService;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddMassTransitWithRabbitMQ(builder.Configuration);
services.AddFanexData(builder.Configuration);

services.Configure<AppSettingsKey>(builder.Configuration);
services.AddHttpClient();
services.AddCoreServices();
services.AddFanexLogSentry(builder.Configuration);
services.AddAutoMapper();
RefDataTypeContainer
    .ConnectTo(new Uri(builder.Configuration.GetSection("Fanex.RemotingRef.Service.Url").Value))
    .FromApplication("Settlement");

AppSettings.AppMailId = builder.Configuration.GetSection("MailServer:Id").Value;
AppSettings.AppMailAddress = builder.Configuration.GetSection("MailServer:Address").Value;



var app = builder.Build();


app.Run(async context =>
{
    await context.Response.WriteAsync("OK");
});
await app.RunAsync();