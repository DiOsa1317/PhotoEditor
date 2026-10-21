using System;
using System.Linq;
using WebApi.Controllers;
using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Repository.Ram;
using Shared.Domain;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dict = new Dictionary<string, User>(){ // для примера
    {"id", new User("login", "id")}
};

var ram = new RamUserRepository(dict);
builder.Services.AddSingleton<IUserRepository>(ram);

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