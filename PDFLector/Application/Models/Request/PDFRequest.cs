using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Models.Request
{
    public class PDFRequest
    {
        public Stream ArchivoStream { get; set; }
        public string NombreArchivo { get; set; }
    }
}
