using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Application.Interfaces;
using Application.Services;
using ImageMagick; // 1. Agregamos el using

var builder = WebApplication.CreateBuilder(args);

// 2. CONFIGURACIÓN GLOBAL DE GHOSTSCRIPT
// Esto le dice a la librería exactamente dónde está el motor en tu Windows
var gsPath = Environment.GetEnvironmentVariable("GHOSTSCRIPT_PATH")
             ?? @"C:\Program Files\gs\gs10.06.0\bin";

// Solo intentamos setear la ruta si estamos en Windows
if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
{
    if (System.IO.Directory.Exists(gsPath))
    {
        ImageMagick.MagickNET.SetGhostscriptDirectory(gsPath);
    }
    else
    {
        Console.WriteLine($"ADVERTENCIA: No se encontró Ghostscript en {gsPath}");
    }
}

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapibuilder.
builder.Services.AddOpenApi();
builder.Services.AddScoped<IPDFLectorService, PDFLectorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();