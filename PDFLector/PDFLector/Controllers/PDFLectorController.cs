using Application.Interfaces;
using Application.Models.Request;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PDFLector.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PDFLectorController : Controller
    {
        private readonly IPDFLectorService _PDFLectorService;
        public PDFLectorController(IPDFLectorService PDFLectorService)
        {
            _PDFLectorService = PDFLectorService;
        }

        
        [EnableRateLimiting("fixed")]
        [AllowAnonymous]
        [HttpPost("leer")]
        public IActionResult LeerPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha enviado un archivo válido.");

            // VALIDACIÓN DE TAMAÑO: 5MB es ideal para la capa gratuita de Railway
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("El archivo es demasiado grande (Máximo 5MB).");

            if (file.ContentType != "application/pdf")
                return BadRequest("El archivo debe ser un formato PDF válido.");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var request = new PDFRequest
                    {
                        ArchivoStream = stream,
                        NombreArchivo = file.FileName
                    };

                    var resultado = _PDFLectorService.Ejecutar(request);

                    // Si el servicio detecta más de 10 páginas, devuelve Exito = false
                    if (!resultado.Exito)
                        return BadRequest(resultado.MensajeError); // Cambiado a BadRequest para que el Front sepa que es error de validación

                    return Ok(resultado);
                }
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}

