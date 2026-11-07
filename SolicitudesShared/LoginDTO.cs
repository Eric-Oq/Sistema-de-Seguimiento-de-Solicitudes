using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolicitudesShared
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "El Usuario es requerido")]
        [StringLength(100, ErrorMessage = "El nombre de usuario no puede superar los 100 caracteres.")]
        public required string NombreUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        public required string password { get; set; }
    }
}
