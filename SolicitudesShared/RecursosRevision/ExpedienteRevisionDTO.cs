using System;

namespace SolicitudesShared.RecursosRevision
{
    public class ExpedienteRevisionDTO
    {
        // IDENTIFICADOR TÉCNICO
        public int IdExpediente { get; set; }

        // ==========================================
        // BLOQUE A: DATOS GENERALES DEL RECURSO
        // ==========================================
        public string? NumeroRecurso { get; set; }
        public string? FolioSolicitud { get; set; }
        public string? MateriaRecurso { get; set; }
        public string? NombreRecurrente { get; set; }
        public DateTime? FechaPresentacion { get; set; }
        public string? Estatus { get; set; }
        public string? ResponsableInterno { get; set; }

        // ==========================================
        // BLOQUE B: DATOS DE LA SOLICITUD ORIGINAL
        // ==========================================
        public string? ContenidoSolicitud { get; set; }
        public DateTime? FechaRespuestaSolicitud { get; set; }
        public string? RespuestaOtorgada { get; set; }
        public string? RazonInterposicion { get; set; }


        // ==========================================
        // BLOQUE C: TRÁMITE DEL RECURSO
        // ==========================================
        public DateTime? FechaAdmision { get; set; }
        public DateTime? FechaNotificacionAdmision { get; set; }
        public DateTime? FechaLimiteContestacion { get; set; }
        public DateTime? FechaContestacionRecurso { get; set; }
        public string? SentidoContestacion { get; set; }
        public DateTime? FechaCierreInstruccion { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string? SentidoResolucion { get; set; }
        public DateTime? FechaNotificacionResolucion { get; set; }

        // ==========================================
        // BLOQUE D: CUMPLIMIENTO
        // ==========================================
        public bool RequiereCumplimiento { get; set; }
        public DateTime? FechaLimiteCumplimiento { get; set; }
        public DateTime? FechaCumplio { get; set; }
        public string? CumplioResolucion { get; set; }
        public string? AcuerdoCumplimiento { get; set; }
        public string? Observaciones { get; set; }

        // ==========================================
        // METADATOS Y VARIABLES AUXILIARES
        // ==========================================
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Cantidad de archivos vinculados (calculada en consultas)
        public int TotalDocumentos { get; set; }
    }
}