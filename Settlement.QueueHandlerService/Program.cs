using Fanex.Data;
using Settlement.Consumer.Models;
using Settlement.QueueHandlerService;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddMassTransitWithRabbitMQ(builder.Configuration);
services.AddFanexData(builder.Configuration);

services.Configure<AppSettingsKey>(builder.Configuration);
services.AddHttpClient();
services.AddCoreServices();
services.AddAutoMapper();



var app = builder.Build();


app.Run();
