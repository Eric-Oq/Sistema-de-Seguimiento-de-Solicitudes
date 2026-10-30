using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolicitudesShared
{
    public class LoginResponse
    {
        public string? Token { get; set; }
        public bool Flag { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
