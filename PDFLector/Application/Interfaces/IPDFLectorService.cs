using Application.Models.Request;
using Application.Models.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IPDFLectorService
    {
        PDFResponse Ejecutar(PDFRequest request);
    }
}
