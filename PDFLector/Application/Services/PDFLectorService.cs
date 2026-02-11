using Application.Interfaces;
using Application.Models.Request;
using Application.Models.Response;
using UglyToad.PdfPig;
using System.Text;
using Tesseract;
using ImageMagick;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class PDFLectorService : IPDFLectorService
    {
        public PDFResponse Ejecutar(PDFRequest request)
        {
            var textoCompleto = new StringBuilder();
            int totalPaginas = 0;

            try
            {
                // 1. LECTURA DIGITAL (PdfPig)
                using (var pdf = PdfDocument.Open(request.ArchivoStream))
                {
                    totalPaginas = pdf.NumberOfPages;

                    // VALIDACIÓN DE PÁGINAS: Detenemos el proceso si es muy largo
                    if (totalPaginas > 10)
                    {
                        return new PDFResponse
                        {
                            Exito = false,
                            MensajeError = $"El PDF tiene {totalPaginas} páginas. El límite para procesamiento es de 10 páginas."
                        };
                    }

                    foreach (var pagina in pdf.GetPages())
                    {
                        textoCompleto.AppendLine(pagina.Text);
                    }
                }

                string resultado = textoCompleto.ToString().Trim();

                // 2. ¿NECESITA OCR?
                if (string.IsNullOrWhiteSpace(resultado))
                {
                    try
                    {
                        request.ArchivoStream.Position = 0; // Volvemos al inicio del stream para leerlo de nuevo
                        resultado = ProcesarConOCR(request.ArchivoStream);
                    }
                    catch (Exception ex)
                    {
                        // Si falla el OCR (por falta de Ghostscript en local), avisamos pero no rompemos
                        return new PDFResponse
                        {
                            Exito = true,
                            TextoExtraido = "AVISO: El PDF es una imagen y el motor de OCR no está disponible.",
                            CantidadPaginas = totalPaginas,
                            MensajeError = ex.Message
                        };
                    }
                }

                return new PDFResponse
                {
                    TextoExtraido = LimpiarTexto(resultado),
                    CantidadPaginas = totalPaginas,
                    Exito = true
                };
            }
            catch (Exception ex)
            {
                return new PDFResponse { Exito = false, MensajeError = "Error al procesar: " + ex.Message };
            }
        }

        private string ProcesarConOCR(Stream stream)
        {
            var textoOcr = new StringBuilder();

            var settings = new MagickReadSettings
            {
                Density = new Density(300, 300)
            };

            using (var images = new MagickImageCollection())
            {
                // Esta línea es la que suele crashear si no hay Ghostscript
                images.Read(stream, settings);

                using (var engine = new TesseractEngine(@"./tessdata", "spa+eng", EngineMode.Default))
                {
                    foreach (var image in images)
                    {
                        image.Format = MagickFormat.Png;
                        using (var pix = Pix.LoadFromMemory(image.ToByteArray()))
                        {
                            using (var page = engine.Process(pix))
                            {
                                textoOcr.AppendLine(page.GetText());
                            }
                        }
                    }
                }
            }
            return textoOcr.ToString();
        }

        private string LimpiarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto;
            string limpio = Regex.Replace(texto, @"[ ]{2,}", " ");
            limpio = Regex.Replace(limpio, @"(\r\n|\n|\r){3,}", "\n\n");
            return limpio.Trim();
        }
    }
}