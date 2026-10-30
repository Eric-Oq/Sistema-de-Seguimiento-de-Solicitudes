using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SolicitudesAPI.Models
{
    public class Denuncia
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de expediente es obligatorio")]
        [StringLength(50)]
        public string NumeroExpediente { get; set; } = string.Empty; // Ej. DEN/0001/2025

        [Required(ErrorMessage = "La razón de interposición es obligatoria")]
        public string RazonInterposicion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fracción y artículo son obligatorios")]
        public string FraccionArticulo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EstatusActual { get; set; } = string.Empty; // Ej. En trámite, Concluido, Prevención

        public DateTime? FechaAdmision { get; set; }

        public DateTime? FechaInformeJustificado { get; set; }

        public string? SentidoInforme { get; set; }

        public DateTime? FechaCierreInstruccion { get; set; }

        public DateTime? FechaResolucion { get; set; }

        public string? SentidoResolucion { get; set; } // Cumple, Incumple

        public DateTime? UltimaFechaEstado { get; set; }

        // Relaciones (Entity Framework) para conectar con las otras tablas
        public virtual ICollection<AcuerdoSeguimiento> Acuerdos { get; set; } = new List<AcuerdoSeguimiento>();
        public virtual ICollection<ExpedienteDigital> Documentos { get; set; } = new List<ExpedienteDigital>();
    
}
}
