using System;

namespace SolicitudesShared.RecursosRevision
{
    public class BitacoraActuacionDTO
    {
        public int IdActuacion { get; set; }
        public int IdExpediente { get; set; }
        public DateTime FechaActuacion { get; set; } = DateTime.Today;
        public string Actuacion { get; set; } = "Admisión";
        public string? Descripcion { get; set; } = string.Empty;
        public string? DocumentoVinculado { get; set; }
        public string Usuario { get; set; } = "Usuario";
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}