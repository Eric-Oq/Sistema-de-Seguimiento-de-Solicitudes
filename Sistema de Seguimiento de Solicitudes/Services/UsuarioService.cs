using SolicitudesShared;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sistema_de_Seguimiento_de_Solicitudes.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly HttpClient _http;

        public UsuarioService(HttpClient http)
        {
            _http = http;
        }

        public async Task<UsuariosDTO> Buscar(int id)
        {
            var response = await _http.GetFromJsonAsync<ResponseAPI<UsuariosDTO>>($"api/Auth/Buscar/{id}​");

            if (response!.Exito)
            {
                return response.Data!;
            }
            else
                throw new Exception(response.Mensaje);
        }
        

        public async Task<bool> Eliminar(int id)
        {
            var result = await _http.DeleteAsync($"api/Auth/Eliminar/{id}");
            var response = await result.Content.ReadFromJsonAsync<ResponseAPI<int>>();

            if (response!.Exito)
            {
                return response.Exito!;
            }
            else
                throw new Exception(response.Mensaje);
        }

        public async Task<List<UsuariosDTO>> Lista_Usuarios()
        {
            var response = await _http.GetFromJsonAsync<ResponseAPI<List<UsuariosDTO>>>("/api/Auth/Lista_Usuarios");

            if (response!.Exito)
            {
                return response.Data!;
            }
            else
                throw new Exception(response.Mensaje);
        }

        public async Task<int> Register(UsuariosDTO usuariosDTO)
        {
            var result = await _http.PostAsJsonAsync("api/Auth/register", usuariosDTO);
            var contentString = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Error HTTP: {result.StatusCode}, Detalles: {contentString}");
            }

            try
            {
                // Intenta parsear como ResponseAPI<int> primero
                var response = JsonSerializer.Deserialize<ResponseAPI<int>>(contentString);

                if (response != null && response.Exito)
                {
                    return response.Data;
                }

                // Si no es ResponseAPI<int>, intenta con el formato simple
                var simpleResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(contentString);

                if (simpleResponse != null && simpleResponse.ContainsKey("message"))
                {
                    // Log de éxito pero devuelve 0 o lanza excepción si necesitas el ID
                    Console.WriteLine(simpleResponse["message"]);
                    return 0; // O maneja de otra forma si necesitas el ID
                }

                throw new Exception("Formato de respuesta no reconocido");
            }
            catch (JsonException jsonEx)
            {
                throw new Exception("Error al procesar la respuesta del servidor", jsonEx);
            }
        }

        public async Task<bool> CambiarContrasena(int usuarioId, string nuevaContrasena)
        {
            var data = new { UsuarioId = usuarioId, NuevaContrasena = nuevaContrasena };
            using var response = await _http.PostAsJsonAsync("api/Auth/CambiarContrasena", data);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ResponseAPI<bool>>()
                ?? throw new InvalidOperationException("Respuesta inválida del servidor.");

            if (!result.Exito)
                throw new InvalidOperationException(result.Mensaje ?? "No se pudo actualizar la contraseña.");

            return result.Data;
        }

    }
}
