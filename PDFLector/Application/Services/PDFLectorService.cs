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
                if (string.IsNullOrWhiteSpace(resultado) || resultado.Length < 100)
                {
                    try
                    {
                        request.ArchivoStream.Position = 0;
                        // Intentamos obtener un resultado más limpio vía OCR
                        string resultadoOCR = ProcesarConOCR(request.ArchivoStream);

                        // Si el OCR devolvió algo sustancial, lo usamos
                        if (!string.IsNullOrWhiteSpace(resultadoOCR))
                        {
                            resultado = resultadoOCR;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Si el OCR falla por Ghostscript, devolvemos lo que PdfPig rescató inicialmente
                        return new PDFResponse
                        {
                            Exito = true,
                            TextoExtraido = LimpiarTexto(resultado) + "\n\n(AVISO: OCR falló, se muestra texto digital base)",
                            CantidadPaginas = totalPaginas,
                            MensajeError = "Detalle OCR: " + ex.Message
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

            // Ruta de los datos de Tesseract
            string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

            // Configuración de resolución (300 DPI es el estándar para buen OCR)
            var settings = new MagickReadSettings { Density = new Density(300, 300) };

            Console.WriteLine("Iniciando conversión de PDF a imagen para OCR...");

            using (var images = new MagickImageCollection())
            {
                // Lee el PDF. Si Ghostscript no está bien enlazado, fallará aquí.
                images.Read(stream, settings);

                // Cargamos los idiomas (español e inglés)
                using (var engine = new TesseractEngine(dataPath, "spa+eng", EngineMode.Default))
                {
                    int paginaActual = 1;
                    foreach (var image in images)
                    {
                        Console.WriteLine($"Procesando página {paginaActual}...");

                        // Optimización: Escala de grises reduce consumo de RAM y mejora la lectura
                        image.Grayscale();

                        // Convertimos la imagen procesada a un formato que Tesseract entienda (Pix)
                        using (var pix = Pix.LoadFromMemory(image.ToByteArray(MagickFormat.Png)))
                        {
                            using (var page = engine.Process(pix))
                            {
                                string textoPagina = page.GetText();
                                textoOcr.AppendLine(textoPagina);
                            }
                        }

                        // Liberar memoria de la imagen inmediatamente
                        image.Dispose();
                        paginaActual++;
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