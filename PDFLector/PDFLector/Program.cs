using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Application.Interfaces;
using Application.Services;
using ImageMagick;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE GHOSTSCRIPT ---
if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
{
    var gsPath = @"C:\Program Files\gs\gs10.06.0\bin";
    if (System.IO.Directory.Exists(gsPath))
    {
        ImageMagick.MagickNET.SetGhostscriptDirectory(gsPath);
    }
}
else
{
    // EN LINUX (RAILWAY): No llamar a SetGhostscriptDirectory.
    // Magick.NET detectará automáticamente el comando 'gs' instalado por apt-get.
    // Esto evita el error de "Invocación" por rutas mal configuradas.
}

// --- RATE LIMITER ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("pdf-limiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Requerido para Swagger en .NET 8
builder.Services.AddSwaggerGen();           // Generador de Swagger
builder.Services.AddScoped<IPDFLectorService, PDFLectorService>();

var app = builder.Build();

// --- PIPELINE DE MIDDLEWARES ---

// Swagger siempre activo para Railway
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty; // Swagger en la raíz
});

app.UseRateLimiter();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();