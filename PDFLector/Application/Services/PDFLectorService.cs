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

                // 2. ¿NECESITA OCR? (Si el texto digital es nulo o muy corto)
                if (string.IsNullOrWhiteSpace(resultado) || resultado.Length < 100)
                {
                    try
                    {
                        request.ArchivoStream.Position = 0;
                        string resultadoOCR = ProcesarConOCR(request.ArchivoStream);

                        if (!string.IsNullOrWhiteSpace(resultadoOCR))
                        {
                            resultado = resultadoOCR;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Si el OCR falla, devolvemos lo que se rescató inicialmente
                        return new PDFResponse
                        {
                            Exito = true,
                            TextoExtraido = LimpiarTexto(resultado) + "\n\n(AVISO: OCR falló, se muestra texto digital base)",
                            CantidadPaginas = totalPaginas,
                            MensajeError = "Detalle OCR: " + ex.Message
                        };
                    }
                }

                // 3. VALIDACIÓN FINAL DE CONTENIDO
                string textoFinal = LimpiarTexto(resultado);

                if (string.IsNullOrWhiteSpace(textoFinal))
                {
                    return new PDFResponse
                    {
                        Exito = false,
                        MensajeError = "No se pudo extraer ningún texto legible del documento. Verifique que el archivo no esté protegido o totalmente en blanco.",
                        TextoExtraido = "",
                        CantidadPaginas = totalPaginas
                    };
                }

                return new PDFResponse
                {
                    TextoExtraido = textoFinal,
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
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

            // Forzamos la ruta de Linux a la ubicación estándar de la versión 5 que instala el Dockerfile
            string dataPath = isWindows
                ? Path.Combine(AppContext.BaseDirectory, "tessdata")
                : "/usr/share/tesseract-ocr/5/tessdata";

            // LOG DE DIAGNÓSTICO: Si falla, esto nos dirá por qué en los logs de Railway
            if (!isWindows)
            {
                if (!Directory.Exists(dataPath))
                {
                    Console.WriteLine($"ERROR CRÍTICO: No existe la ruta {dataPath}");
                    // Intento de rescate si Debian instaló la v4
                    if (Directory.Exists("/usr/share/tesseract-ocr/4.00/tessdata"))
                        dataPath = "/usr/share/tesseract-ocr/4.00/tessdata";
                }
                ImageMagick.MagickNET.SetGhostscriptDirectory("/usr/lib/x86_64-linux-gnu");
            }

            using (var images = new MagickImageCollection())
            {
                if (stream.CanSeek) stream.Position = 0;

                var settings = new MagickReadSettings { Density = new Density(300, 300) };
                images.Read(stream, settings);

                // USAMOS UN TRY-CATCH ESPECÍFICO AQUÍ PARA VER EL ERROR REAL
                try
                {
                    using (var engine = new TesseractEngine(dataPath, "spa+eng", EngineMode.Default))
                    {
                        foreach (var image in images)
                        {
                            image.ColorType = ColorType.Bilevel;
                            using (var pix = Pix.LoadFromMemory(image.ToByteArray(MagickFormat.Png)))
                            {
                                using (var page = engine.Process(pix))
                                {
                                    textoOcr.AppendLine(page.GetText());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Esto enviará el detalle técnico real al JSON de respuesta
                    throw new Exception($"Fallo interno Tesseract en {dataPath}. Detalle: {ex.Message} -> {ex.InnerException?.Message}");
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