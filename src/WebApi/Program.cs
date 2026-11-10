using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Mongo;
using Shared.Infrastructure.Repository.Ram;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
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

builder.Services.AddSingleton<IUserRepository, RamUserRepository>();
builder.Services.AddSingleton<ISessionRepository, RamSessionRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();


app.UseAuthorization();
    
app.MapControllers();
// app.MapControllerRoute(
//     name : "default",
//     pattern : "{}"
// );

app.Run();