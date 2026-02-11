using Application.Interfaces;
using Application.Models.Request;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [AllowAnonymous]
        [HttpPost("leer")]
        public IActionResult LeerPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha enviado un archivo válido.");
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("Archivo demasiado grande.");
            if (file.ContentType != "application/pdf")
                return BadRequest("El archivo no es un PDF real.");
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
                    if (!resultado.Exito)
                        return StatusCode(500, resultado.MensajeError);
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

