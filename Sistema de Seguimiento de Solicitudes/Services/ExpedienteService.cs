using SolicitudesShared;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sistema_de_Seguimiento_de_Solicitudes.Services
{
    public class ExpedienteService : IExpedienteService
    {
        private readonly HttpClient _http;

        public ExpedienteService(HttpClient http)
        {
            _http = http;
        }

        // Obtener lista completa de expedientes
        public async Task<List<ExpedienteDTO>> Lista()
        {
            var response = await _http.GetFromJsonAsync<ResponseAPI<List<ExpedienteDTO>>>("api/Expedientes/Lista");
            return response != null && response.Exito ? response.Data! : new List<ExpedienteDTO>();
        }

        // Buscar un expediente por ID
        public async Task<ExpedienteDTO> Buscar(int id)
        {
            var response = await _http.GetFromJsonAsync<ResponseAPI<ExpedienteDTO>>($"api/Expedientes/Buscar/{id}");
            return response != null && response.Exito ? response.Data! : new ExpedienteDTO();
        }

        // Crear un expediente
        public async Task<int> Crear(ExpedienteDTO expediente)
        {
            var response = await _http.PostAsJsonAsync("api/Expedientes/Crear", expediente);
            var result = await response.Content.ReadFromJsonAsync<ResponseAPI<int>>();

            return result != null && result.Exito ? result.Data : 0;
        }

        // Eliminar un expediente
        public async Task<bool> Eliminar(int id)
        {
            var response = await _http.DeleteAsync($"api/Expedientes/Eliminar/{id}");
            var result = await response.Content.ReadFromJsonAsync<ResponseAPI<int>>();

            return result != null && result.Exito;
        }

        // Actualizar un expediente
        public async Task<bool> Actualizar(ExpedienteDTO expediente)
        {
            var response = await _http.PutAsJsonAsync($"api/Expedientes/Actualizar/{expediente.Id}", expediente);
            var result = await response.Content.ReadFromJsonAsync<ResponseAPI<int>>();

            return result != null && result.Exito;
        }

        // Buscar por folio o contenido (nuevo)
        public async Task<List<ExpedienteDTO>> BuscarPorTexto(string filtro)
        {
            var response = await _http.GetFromJsonAsync<ResponseAPI<List<ExpedienteDTO>>>(
                $"api/Expedientes/BuscarPorTexto?filtro={filtro}"
            );

            return response != null && response.Exito ? response.Data! : new List<ExpedienteDTO>();
        }
    }
}
