using MassTransit;
using MongoDB.Driver;
using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Mongo;
using Worker.Infrastructure.Consumers;
using Worker.Infrastructure.Interfaces;
using Worker.Infrastructure.Magick;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoDatabase>(_ =>
{
    var client = new MongoClient(builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017");
    return client.GetDatabase("pixelqueue");
});

builder.Services.AddSingleton<ITaskRepository, MongoTaskRepository>();
builder.Services.AddSingleton<IFileStorage, MongoFileStorage>();
builder.Services.AddSingleton<IImageProcessor, MagickImageProcessor>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskCreatedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", host =>
        {
            host.Username("guest");
            host.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGet("/", () => "PixelQueue worker");

app.Run();