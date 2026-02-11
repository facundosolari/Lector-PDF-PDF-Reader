using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Application.Interfaces;
using Application.Services;
using ImageMagick;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE GHOSTSCRIPT ---
var gsPath = Environment.GetEnvironmentVariable("GHOSTSCRIPT_PATH") ?? @"C:\Program Files\gs\gs10.06.0\bin";
if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
{
    if (System.IO.Directory.Exists(gsPath))
    {
        ImageMagick.MagickNET.SetGhostscriptDirectory(gsPath);
    }
}

// --- RATE LIMITER ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("pdf-limiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5; // Máximo 5 PDFs por minuto
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(); // Esto genera el v1.json
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Necesario para la UI
builder.Services.AddScoped<IPDFLectorService, PDFLectorService>();

var app = builder.Build();

// --- PIPELINE DE MIDDLEWARES ---


// 1. Swagger configurado con la ruta correcta para AddSwaggerGen
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // CAMBIO AQUÍ: La ruta correcta para SwaggerGen es /swagger/v1/swagger.json
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");

    options.RoutePrefix = string.Empty;
});

// 2. Rate Limiter
app.UseRateLimiter();

app.UseHttpsRedirection();

// 3. Controladores
app.MapControllers();

app.Run();