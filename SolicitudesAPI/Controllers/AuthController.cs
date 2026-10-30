using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SolicitudesAPI.Models;
using SolicitudesShared;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Seguimiento_de_Solicitudes.Models;
using static Sistema_de_Seguimiento_de_Solicitudes.Models.CambiarContrasena;

namespace SolicitudesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SistemaSolicitudesContext _context;

        private readonly IConfiguration _configuration;



        public AuthController(SistemaSolicitudesContext context, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpGet("Lista_Usuarios")]
        public async Task<IActionResult> Lista_Usuarios()
        {
            var responseApi = new ResponseAPI<List<UsuariosDTO>>();
            var listaUsuariosDTO = new List<UsuariosDTO>();

            try
            {
                foreach (var usuarios in await _context.Usuarios.ToListAsync())
                {
                    listaUsuariosDTO.Add(new UsuariosDTO
                    {
                        Id = usuarios.Id,
                        NombreUsuario = usuarios.NombreUsuario,
                        password = usuarios.password,
                        Rol = usuarios.Rol
                    });
                }
                responseApi.Exito = true;
                responseApi.Data = listaUsuariosDTO;
            }
            catch (Exception ex)
            {
                responseApi.Exito = false;
                responseApi.Mensaje = ex.Message;
            }
            return Ok(responseApi);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UsuariosDTO usuariosDTO)
        {
            var user = new Usuario
            {
                NombreUsuario = usuariosDTO.NombreUsuario,
                Rol = usuariosDTO.Rol
            };
            user.password = new PasswordHasher<Usuario>().HashPassword(user, usuariosDTO.password);

            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            if (user.Id != 0)
            {
                return Ok(new { message = "Usuario creado con éxito" });
            }
            else
            {
                return BadRequest(new { message = "Error al crear el usuario" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == loginDTO.NombreUsuario);

            if (user == null)
            {
                //return Unauthorized(new { message = "Usuario o contraseña incorrectos" });
                return Unauthorized(new { message = "Usuario o contraseña incorrectos" });
            }

            var passwordHasher = new PasswordHasher<Usuario>();
            var result = passwordHasher.VerifyHashedPassword(user, user.password, loginDTO.password);

            if (result == PasswordVerificationResult.Success)
            {
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.NombreUsuario),
                    new Claim(ClaimTypes.Role, user.Rol),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = GenerarToken(authClaims);

                //return Ok(new { token });
                return Ok(new LoginResponse { Token = token, Flag = true, Message = "Login successful" });
            }
            else
            {
                //return Unauthorized(new { message = "Usuario o contraseña incorrectos" });
                return Unauthorized(new LoginResponse { Message = "Usuario o contraseña incorrectos" });
            }
        }

        [HttpGet("Buscar/{id}")]
        public async Task<ActionResult> Buscar(int id)
        {
            var responseApi = new ResponseAPI<UsuariosDTO>();
            //var UsuariosDTO = new UsuariosDTO();

            try
            {
                var dbUsuarios = await _context.Usuarios.FirstOrDefaultAsync(e => e.Id == id);

                if (dbUsuarios != null)
                {
                    var UsuariosDTO = new UsuariosDTO
                    {
                        NombreUsuario = dbUsuarios.NombreUsuario,
                        password = dbUsuarios.password,
                        Rol = dbUsuarios.Rol
                    };

                    responseApi.Exito = true;
                    responseApi.Data = UsuariosDTO;
                }
                else
                {
                    responseApi.Exito = false;
                    responseApi.Mensaje = "No se encontró el expediente";
                }
            }
            catch (Exception ex)
            {
                responseApi.Exito = false;
                responseApi.Mensaje = ex.Message;
            }

            return Ok(responseApi);
        }

        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var responseApi = new ResponseAPI<int>();

            try
            {
                var dbUsuario = await _context.Usuarios.FirstOrDefaultAsync(e => e.Id == id);

                if (dbUsuario != null)
                {
                    _context.Usuarios.Remove(dbUsuario);
                    await _context.SaveChangesAsync();

                    responseApi.Exito = true;
                }
                else
                {
                    responseApi.Exito = false;
                    responseApi.Mensaje = "No se pudo encontrar el expediente";
                }
            }
            catch (Exception ex)
            {
                responseApi.Exito = false;
                responseApi.Mensaje = ex.Message;
            }

            return Ok(responseApi);
        }

        [HttpPost("CambiarContrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null)
                return NotFound(new ResponseAPI<bool> { Exito = false, Mensaje = "Usuario no encontrado." });

            var passwordHasher = new PasswordHasher<Usuario>();
            usuario.password = passwordHasher.HashPassword(usuario, request.NuevaContrasena);

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return Ok(new ResponseAPI<bool> { Exito = true, Data = true, Mensaje = "Contraseña actualizada correctamente." });
        }

        private string GenerarToken(List<Claim> claims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]!));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.Now.AddHours(2),
                claims: claims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
