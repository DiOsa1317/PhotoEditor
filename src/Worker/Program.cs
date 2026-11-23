using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Mongo;
using Worker.Infrastructure.BackgroundServices;
using Worker.Infrastructure.Consumers;
using Worker.Infrastructure.Interfaces;
using Worker.Infrastructure.Magick;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, cfg) => cfg
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

var mongoConn = builder.Configuration["MONGO_CONNECTION_STRING"] ?? "mongodb://localhost:27017";
var mongoDb = builder.Configuration["MONGO_DATABASE"] ?? "pixelqueue";

builder.Services.AddSingleton<IMongoDatabase>(_ =>
{
    var client = new MongoClient(mongoConn);
    return client.GetDatabase(mongoDb);
});

builder.Services.AddSingleton<ITaskRepository, MongoTaskRepository>();
builder.Services.AddSingleton<IFileStorage, MongoFileStorage>();
builder.Services.AddSingleton<IImageProcessor, MagickImageProcessor>();

builder.Services.AddHostedService<LeaseRecoveryService>();

var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? "localhost";
var rabbitPort = int.Parse(builder.Configuration["RABBITMQ_PORT"] ?? "5672");
var rabbitUser = builder.Configuration["RABBITMQ_USER"] ?? "guest";
var rabbitPass = builder.Configuration["RABBITMQ_PASS"] ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskCreatedConsumer>(cfg =>
        { cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); });
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri($"rabbitmq://{rabbitHost}:{rabbitPort}"), h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "worker" }));

app.MapGet("/ready", async (IMongoDatabase db) =>
{
    try
    {
        await db.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

app.MapGet("/", () => "PixelQueue worker");
app.MapMetrics();

app.Run();