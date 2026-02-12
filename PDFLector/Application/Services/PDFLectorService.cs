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
            string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
            Console.WriteLine($"[DEBUG] Directorio Base: {AppContext.BaseDirectory}");
            Console.WriteLine($"[DEBUG] ¿Existe tessdata?: {Directory.Exists(dataPath)}");

            // 1. FORZAR RUTA EN LINUX (Soluciona el error de Invocación)
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                // Esta es la ruta estándar donde apt-get instala las librerías en Debian
                ImageMagick.MagickNET.SetGhostscriptDirectory("/usr/lib/x86_64-linux-gnu");
            }

            var settings = new MagickReadSettings { Density = new Density(300, 300) };

            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                // Esta es la ruta donde Railway instala las librerías nativas
                MagickNET.SetGhostscriptDirectory("/usr/lib/x86_64-linux-gnu");
            }
            using (var images = new MagickImageCollection())
            {
                // 2. ASEGURAR POSICIÓN DEL STREAM
                if (stream.CanSeek) stream.Position = 0;

                images.Read(stream, settings);

                using (var engine = new TesseractEngine(dataPath, "spa+eng", EngineMode.Default))
                {
                    foreach (var image in images)
                    {
                        // 1. Convertir a Blanco y Negro puro (elimina el error de spp/bps)
                        image.ColorType = ColorType.Bilevel;
                        image.Settings.Compression = CompressionMethod.NoCompression;

                        // 2. Usar formato PNG (es el más compatible con Tesseract)
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