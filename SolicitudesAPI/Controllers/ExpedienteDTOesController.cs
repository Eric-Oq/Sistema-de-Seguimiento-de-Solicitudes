using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SolicitudesAPI.Models;
using SolicitudesShared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SolicitudesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpedientesController : ControllerBase
    {
        private readonly SistemaSolicitudesContext _context;

        public ExpedientesController(SistemaSolicitudesContext context)
        {
            _context = context;
        }

        [HttpGet("Lista")]
        public async Task<ActionResult> Lista()
        {
            var responseApi = new ResponseAPI<List<ExpedienteDTO>>();
            var listaExpedienteDTO = new List<ExpedienteDTO>();

            try
            {
                foreach (var expediente in await _context.Expedientes.ToListAsync())
                {
                    listaExpedienteDTO.Add(new ExpedienteDTO
                    {
                        Id = expediente.Id,
                        Folio = expediente.Folio,
                        // Dentro del foreach de Lista
                        AnoAdmision = expediente.AnoAdmision ,
                        NombreSolicitante = expediente.NombreSolicitante,
                        FechaInicio = expediente.FechaInicio ?? default(DateTime),
                        Estado = expediente.Estado,
                        ContenidoSolicitud = expediente.ContenidoSolicitud,
                        MesAdmision = expediente.MesAdmision,
                        TipoSolicitud = expediente.TipoSolicitud,
                        TipoDerecho = expediente.TipoDerecho,
                        FechaLimiteRespuesta10dias = expediente.FechaLimiteRespuesta10dias,
                        Ampliacion = expediente.Ampliacion,
                        NumeroSesionComiteAmpliacion = expediente.NumeroSesionComiteAmpliacion,
                        FechaSesionComiteAmpliacion = expediente.FechaSesionComiteAmpliacion,
                        FechaLimiteRespuesta20dias = expediente.FechaLimiteRespuesta20dias,
                        FechaRespuesta = expediente.FechaRespuesta,
                        PromedioDiasRespuesta = expediente.PromedioDiasRespuesta,
                        Prevencion = expediente.Prevencion,
                        SubsanaPrevencionReinicoTramite = expediente.SubsanaPrevencionReinicoTramite,
                        FechaLimitePrevencion10dias = expediente.FechaLimitePrevencion10dias,
                        RecibidaRegistrada = expediente.RecibidaRegistrada,
                        MedioRecepcionSolicitudManual = expediente.MedioRecepcionSolicitudManual,
                        ComoDeseaRecibirRespuestaPersonaSolicitante = expediente.ComoDeseaRecibirRespuestaPersonaSolicitante,
                        CorreoElectronicoSolicitante = expediente.CorreoElectronicoSolicitante,
                        AreaPoseedoraInformacion = expediente.AreaPoseedoraInformacion,
                        Materia = expediente.Materia,
                        CiudadSolicitante = expediente.CiudadSolicitante,
                        Tematica = expediente.Tematica,
                        TematicaEspecifica = expediente.TematicaEspecifica,
                        SentidoRespuesta = expediente.SentidoRespuesta,
                        PrecisionSentidoRespuesta = expediente.PrecisionSentidoRespuesta,
                        ModalidadEntrega = expediente.ModalidadEntrega,
                        Cobro = expediente.Cobro,
                        RecursoRevision = expediente.RecursoRevision,
                        NumeroRecursoRevision = expediente.NumeroRecursoRevision,
                        DatosRecursoRevision = expediente.DatosRecursoRevision,
                        Nota = expediente.Nota,
                        
                    });
                }
                responseApi.Exito = true;
                responseApi.Data = listaExpedienteDTO;
            }
            catch (Exception ex)
            {
                responseApi.Exito = false;
                responseApi.Mensaje = ex.Message;
            }
            return Ok(responseApi);
        }

        [HttpPost("Crear")]
        [Authorize(Roles = "ADMINISTRADOR,SUPERVISIÓN,CAPTURA")]
        public async Task<ActionResult> Crear(ExpedienteDTO expediente)
        {
            var responseApi = new ResponseAPI<int>();
            try
            {
                // 🔥 AQUÍ ESTABA EL ERROR: Ahora mapeamos TODOS los campos al crear 🔥
                var dbExpediente = new Expediente
                {
                    Folio = expediente.Folio,
                    NombreSolicitante = expediente.NombreSolicitante,
                    FechaInicio = expediente.FechaInicio,
                    Estado = expediente.Estado,
                    ContenidoSolicitud = expediente.ContenidoSolicitud,
                    MesAdmision = expediente.MesAdmision,
                    TipoSolicitud = expediente.TipoSolicitud,
                    TipoDerecho = expediente.TipoDerecho,
                    FechaLimiteRespuesta10dias = expediente.FechaLimiteRespuesta10dias,
                    Ampliacion = expediente.Ampliacion,
                    NumeroSesionComiteAmpliacion = expediente.NumeroSesionComiteAmpliacion,
                    FechaSesionComiteAmpliacion = expediente.FechaSesionComiteAmpliacion,
                    FechaLimiteRespuesta20dias = expediente.FechaLimiteRespuesta20dias,
                    FechaRespuesta = expediente.FechaRespuesta,
                    PromedioDiasRespuesta = expediente.PromedioDiasRespuesta,
                    Prevencion = expediente.Prevencion,
                    SubsanaPrevencionReinicoTramite = expediente.SubsanaPrevencionReinicoTramite,
                    FechaLimitePrevencion10dias = expediente.FechaLimitePrevencion10dias,
                    RecibidaRegistrada = expediente.RecibidaRegistrada,
                    MedioRecepcionSolicitudManual = expediente.MedioRecepcionSolicitudManual,
                    ComoDeseaRecibirRespuestaPersonaSolicitante = expediente.ComoDeseaRecibirRespuestaPersonaSolicitante,
                    CorreoElectronicoSolicitante = expediente.CorreoElectronicoSolicitante,
                    AreaPoseedoraInformacion = expediente.AreaPoseedoraInformacion,
                    Materia = expediente.Materia,
                    CiudadSolicitante = expediente.CiudadSolicitante,
                    Tematica = expediente.Tematica,
                    TematicaEspecifica = expediente.TematicaEspecifica,
                    SentidoRespuesta = expediente.SentidoRespuesta,
                    PrecisionSentidoRespuesta = expediente.PrecisionSentidoRespuesta,
                    ModalidadEntrega = expediente.ModalidadEntrega,
                    Cobro = expediente.Cobro,
                    RecursoRevision = expediente.RecursoRevision,
                    NumeroRecursoRevision = expediente.NumeroRecursoRevision,
                    DatosRecursoRevision = expediente.DatosRecursoRevision,
                    Nota = expediente.Nota
                };

                _context.Expedientes.Add(dbExpediente);
                await _context.SaveChangesAsync();

                responseApi.Exito = true;
                responseApi.Data = dbExpediente.Id;
            }
            catch (Exception ex)
            {
                responseApi.Exito = false;
                responseApi.Mensaje = ex.Message;
            }
            return Ok(responseApi);
        }

        [HttpPut("Actualizar/{id}")]
        [Authorize(Roles = "ADMINISTRADOR,SUPERVISIÓN,CAPTURA")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ExpedienteDTO expediente)
        {
            if (id != expediente.Id) return BadRequest("ID mismatch");

            var expedienteExistente = await _context.Expedientes.FindAsync(id);
            if (expedienteExistente == null) return NotFound("Expediente no encontrado");

            // 🔥 Actualización de campos (Incluyendo el Folio que antes no se guardaba)
            expedienteExistente.Folio = expediente.Folio;
            expedienteExistente.AnoAdmision = expediente.AnoAdmision; 
            expedienteExistente.NombreSolicitante = expediente.NombreSolicitante;
            expedienteExistente.MesAdmision = expediente.MesAdmision;
            expedienteExistente.TipoSolicitud = expediente.TipoSolicitud;
            expedienteExistente.TipoDerecho = expediente.TipoDerecho;
            expedienteExistente.FechaInicio = expediente.FechaInicio;
            expedienteExistente.FechaLimiteRespuesta10dias = expediente.FechaLimiteRespuesta10dias;
            expedienteExistente.Ampliacion = expediente.Ampliacion;
            expedienteExistente.NumeroSesionComiteAmpliacion = expediente.NumeroSesionComiteAmpliacion;
            expedienteExistente.FechaSesionComiteAmpliacion = expediente.FechaSesionComiteAmpliacion;
            expedienteExistente.FechaLimiteRespuesta20dias = expediente.FechaLimiteRespuesta20dias;
            expedienteExistente.Estado = expediente.Estado;
            expedienteExistente.FechaRespuesta = expediente.FechaRespuesta;
            expedienteExistente.PromedioDiasRespuesta = expediente.PromedioDiasRespuesta;
            expedienteExistente.Prevencion = expediente.Prevencion;
            expedienteExistente.SubsanaPrevencionReinicoTramite = expediente.SubsanaPrevencionReinicoTramite;
            expedienteExistente.FechaLimitePrevencion10dias = expediente.FechaLimitePrevencion10dias;
            expedienteExistente.RecibidaRegistrada = expediente.RecibidaRegistrada;
            expedienteExistente.MedioRecepcionSolicitudManual = expediente.MedioRecepcionSolicitudManual;
            expedienteExistente.ComoDeseaRecibirRespuestaPersonaSolicitante = expediente.ComoDeseaRecibirRespuestaPersonaSolicitante;
            expedienteExistente.CorreoElectronicoSolicitante = expediente.CorreoElectronicoSolicitante;
            expedienteExistente.ContenidoSolicitud = expediente.ContenidoSolicitud;
            expedienteExistente.AreaPoseedoraInformacion = expediente.AreaPoseedoraInformacion;
            expedienteExistente.Materia = expediente.Materia;
            expedienteExistente.CiudadSolicitante = expediente.CiudadSolicitante;
            expedienteExistente.Tematica = expediente.Tematica;
            expedienteExistente.TematicaEspecifica = expediente.TematicaEspecifica;
            expedienteExistente.SentidoRespuesta = expediente.SentidoRespuesta;
            expedienteExistente.PrecisionSentidoRespuesta = expediente.PrecisionSentidoRespuesta;
            expedienteExistente.ModalidadEntrega = expediente.ModalidadEntrega;
            expedienteExistente.Cobro = expediente.Cobro;
            expedienteExistente.RecursoRevision = expediente.RecursoRevision;
            expedienteExistente.NumeroRecursoRevision = expediente.NumeroRecursoRevision;
            expedienteExistente.DatosRecursoRevision = expediente.DatosRecursoRevision;
            expedienteExistente.Nota = expediente.Nota;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ResponseAPI<int> { Exito = true, Data = expedienteExistente.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("Eliminar/{id}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var responseApi = new ResponseAPI<int>();
            try
            {
                var dbExpediente = await _context.Expedientes.FirstOrDefaultAsync(e => e.Id == id);
                if (dbExpediente != null)
                {
                    _context.Expedientes.Remove(dbExpediente);
                    await _context.SaveChangesAsync();
                    responseApi.Exito = true;
                }
                else
                {
                    responseApi.Exito = false;
                    responseApi.Mensaje = "No encontrado";
                }
            }
            catch (Exception ex)
            {
                responseApi.Exito = false;
                responseApi.Mensaje = ex.Message;
            }
            return Ok(responseApi);
        }

        [HttpGet("BuscarPorTexto")]
        public async Task<ActionResult> BuscarPorTexto([FromQuery] string filtro)
        {
            var responseApi = new ResponseAPI<List<ExpedienteDTO>>();
            var listaExpedienteDTO = new List<ExpedienteDTO>();

            try
            {
                var query = _context.Expedientes.AsQueryable();
                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    filtro = filtro.ToLower();
                    query = query.Where(e =>
                        (e.Folio != null && e.Folio.ToLower().Contains(filtro)) ||
                        (e.NombreSolicitante != null && e.NombreSolicitante.ToLower().Contains(filtro)) ||
                        (e.Estado != null && e.Estado.ToLower().Contains(filtro)) ||
                        (e.ContenidoSolicitud != null && e.ContenidoSolicitud.ToLower().Contains(filtro))
                    );
                }

                var expedientes = await query.ToListAsync();
                foreach (var expediente in expedientes)
                {
                    listaExpedienteDTO.Add(new ExpedienteDTO
                    {
                        Id = expediente.Id,
                        Folio = expediente.Folio,
                        NombreSolicitante = expediente.NombreSolicitante,
                        FechaInicio = expediente.FechaInicio ?? default(DateTime),
                        Estado = expediente.Estado,
                        ContenidoSolicitud = expediente.ContenidoSolicitud
                    });
                }
                responseApi.Exito = true;
                responseApi.Data = listaExpedienteDTO;
            }
            catch (Exception ex)
            {
                responseApi.Exito = false;
                responseApi.Mensaje = ex.Message;
            }
            return Ok(responseApi);
        }
    }
}