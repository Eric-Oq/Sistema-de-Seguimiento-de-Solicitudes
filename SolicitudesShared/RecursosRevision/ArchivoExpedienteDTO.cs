using System;

namespace SolicitudesShared.RecursosRevision
{
    public class ArchivoExpedienteDTO
    {
        public int IdArchivo { get; set; }
        public int IdExpediente { get; set; }
        public string TipoDocumento { get; set; } = "Acuerdo de admisión";
        public DateTime FechaDocumento { get; set; } = DateTime.Today;
        public string Descripcion { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = "Usuario";
        public DateTime? FechaRegistro { get; set; }
    }
}