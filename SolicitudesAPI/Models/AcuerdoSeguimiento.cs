using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolicitudesAPI.Models
{
    public class AcuerdoSeguimiento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DenunciaId { get; set; }

        [Required(ErrorMessage = "La fecha del acuerdo es obligatoria")]
        public DateTime FechaAcuerdo { get; set; }

        [Required(ErrorMessage = "El contenido del acuerdo es obligatorio")]
        public string ContenidoAcuerdo { get; set; } = string.Empty;

        // Navegación (Indica que este acuerdo pertenece a una Denuncia específica)
        [ForeignKey("DenunciaId")]
        public virtual Denuncia Denuncia { get; set; } = null!;
    }
}
