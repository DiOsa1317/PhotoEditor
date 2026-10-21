using Worker.Infrastructure.Interfaces;
using Worker.Infrastructure.Magick;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IImageProcessor, MagickImageProcessor>();

var app = builder.Build();

app.MapGet("/", () => "PixelQueue worker");

app.Run();