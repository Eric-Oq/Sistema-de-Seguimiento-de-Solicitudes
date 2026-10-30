using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolicitudesAPI.Models
{
    public class ExpedienteDigital
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DenunciaId { get; set; }

        [Required]
        [StringLength(255)]
        public string NombreDocumento { get; set; } = string.Empty; // Ej. "Acuerdo_Admision.pdf"

        [Required]
        public string RutaArchivo { get; set; } = string.Empty; // La ubicación en tu servidor o nube

        public DateTime FechaCarga { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("DenunciaId")]
        public virtual Denuncia Denuncia { get; set; } = null!;
    }
}
