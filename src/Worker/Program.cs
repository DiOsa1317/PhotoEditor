using MassTransit;
using MongoDB.Driver;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Mongo;
using Worker.Infrastructure.Consumers;
using Worker.Infrastructure.Interfaces;
using Worker.Infrastructure.Magick;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, cfg) => cfg
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("worker"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options => options.Filter = context =>
            !context.Request.Path.StartsWithSegments("/metrics"))
        .AddSource("MassTransit")
        .AddSource("MongoDB.Driver")
        .AddOtlpExporter());

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseHttpMetrics();

app.MapGet("/", () => "PixelQueue worker");
app.MapMetrics();

app.Run();