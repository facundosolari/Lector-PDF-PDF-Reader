using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Models.Response
{
    public class PDFResponse
    {
        public string TextoExtraido { get; set; }
        public int CantidadPaginas { get; set; }
        public bool Exito { get; set; }
        public string MensajeError { get; set; }
    }
}
