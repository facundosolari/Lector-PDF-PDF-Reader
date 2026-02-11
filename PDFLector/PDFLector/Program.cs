using Microsoft.AspNetCore.RateLimiting; // 1. Nuevo usando para el límite
using System.Threading.RateLimiting;
using Application.Interfaces;
using Application.Services;
using ImageMagick;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE GHOSTSCRIPT (Tu código existente) ---
var gsPath = Environment.GetEnvironmentVariable("GHOSTSCRIPT_PATH") ?? @"C:\Program Files\gs\gs10.06.0\bin";
if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
{
    if (System.IO.Directory.Exists(gsPath))
    {
        ImageMagick.MagickNET.SetGhostscriptDirectory(gsPath);
    }
}

// 2. CONFIGURACIÓN DEL RATE LIMITER (Agregalo aquí)
builder.Services.AddRateLimiter(options =>
{
    // Definimos una política llamada "pdf-limiter"
    options.AddFixedWindowLimiter("pdf-limiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1); // Ventana de tiempo: 1 minuto
        opt.PermitLimit = 5;                  // Solo 5 PDF por minuto por IP
        opt.QueueLimit = 0;                   // No encolar, rechazar de inmediato
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Respuesta personalizada cuando alguien se pasa del límite
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Services (Tu código existente)
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IPDFLectorService, PDFLectorService>();

var app = builder.Build();

// 3. ACTIVAR EL MIDDLEWARE (Importante: debe ir antes de MapControllers)
app.UseRateLimiter();

// Swagger y pipeline (Tu código existente)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();