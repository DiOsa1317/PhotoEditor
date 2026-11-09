using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Repository.Ram;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IUserRepository, RamUserRepository>();
builder.Services.AddSingleton<ISessionRepository, RamSessionRepository>();
builder.Services.AddSingleton<IPhotoRepository, RamPhotoRepository>();

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