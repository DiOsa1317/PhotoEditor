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
using Shared.Infrastructure.Repository.Ram;


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, cfg) => cfg
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));


builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:5228")
    .AllowAnyHeader()
    .AllowAnyMethod()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoClient = new MongoClient(builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017");
var mongoDatabase = mongoClient.GetDatabase(builder.Configuration["Mongo:Database"] ?? "pixelqueue");


builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddSingleton<ITaskRepository, MongoTaskRepository>();
builder.Services.AddSingleton<IFileStorage, MongoFileStorage>();
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("webapi"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options => options.Filter = context =>
            !context.Request.Path.StartsWithSegments("/metrics") &&
            !context.Request.Path.StartsWithSegments("/swagger"))
        .AddSource("MassTransit")
        .AddSource("MongoDB.Driver")
        .AddOtlpExporter());

builder.Services.AddSingleton<IUserRepository, RamUserRepository>();
builder.Services.AddSingleton<ISessionRepository, RamSessionRepository>();

var app = builder.Build();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseHttpMetrics();


app.UseAuthorization();
    
app.MapControllers();
// app.MapControllerRoute(
//     name : "default",
//     pattern : "{}"
// );
app.MapMetrics();

app.Run();