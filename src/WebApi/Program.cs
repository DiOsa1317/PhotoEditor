using System;
using System.Linq;
using WebApi.Controllers;
using Infrastructure.Interfaces;
using Infrastructure.Repository.Ram;
using WebApi.Domain;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var repo = new RamUserRepository(new Dictionary<string, User>());
builder.Services.AddSingleton<IUserRepository>(repo);

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