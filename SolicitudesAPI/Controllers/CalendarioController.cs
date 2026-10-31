using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Seguimiento_de_Solicitudes.Models;
using SolicitudesAPI.Models;
using SolicitudesShared;
using System.Collections.Generic;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarioController : ControllerBase
{
    private readonly SistemaSolicitudesContext _context;

    public CalendarioController(SistemaSolicitudesContext context)
    {
        _context = context;
    }

    [HttpGet("Obtener_dias")]
    public async Task<ActionResult<List<CalendarioDTO>>> Obtener()
    {
        var dias = await _context.Calendario.
            Select(d => new CalendarioDTO
            {
                Fecha = d.Fecha,
                Activo = d.Activo
            }).ToListAsync();


        return Ok(dias);
    }

    [HttpPost("Guardar_dia")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<ActionResult> Guardar(List<CalendarioDTO> diasActivos)
    {
        var responseApi = new ResponseAPI<List<CalendarioDTO>>();

        if(diasActivos == null || diasActivos.Count == 0)
        {
            responseApi.Exito = false;
            responseApi.Mensaje = "No se recibieron días activos.";
            return BadRequest(responseApi);
        }

        // Guardar los días activos en la base de datos
        foreach (var dia in diasActivos)
        {
            // Verificar si el día ya existe en la base de datos

            bool existe = await _context.Calendario
                .AnyAsync(d => d.Fecha == dia.Fecha);

            if (!existe)
            {
                var nuevoDia = new Calendario
                {
                    Fecha = dia.Fecha,
                    Activo = dia.Activo
                };

                _context.Calendario.Add(nuevoDia);
            }
        }

        await _context.SaveChangesAsync();

        //DiasActivos = diasActivos;

        return Ok(new { mensaje = "Dias guardados correctamente"});
    }

    [HttpGet ("Get_DiasInhabiles")]
    public async Task<ActionResult<List<DiaInhabilManualDTO>>> ObtenerDiasInhabiles()
    {
        var dias = await _context.DiaInhabilManual
            .Select(d => new DiaInhabilManualDTO
            {
                Fecha = d.Fecha.Date
            })
            .ToListAsync();

        return Ok(dias);
    }

    // guardar dia uno por uno
    [HttpPost ("Guardar_DiasInhabiles")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Guardar_DiaInhabil([FromBody] DiaInhabilManualDTO diaInhabil)
    {
        //fecha = fecha.Date;

        if (diaInhabil == null || diaInhabil == default)
        {
            return BadRequest("La fecha es requerida.");
        }

        var fecha = diaInhabil.Fecha.Date;

        if (!await _context.DiaInhabilManual.AnyAsync(d => d.Fecha == fecha))
        {
            _context.DiaInhabilManual.Add(new DiaInhabilManual { Fecha = fecha });
            await _context.SaveChangesAsync();

            await RecalcularFechasLimiteEnSolicitudes();
        }

        return Ok(new { mensaje = "Día inhábil guardado correctamente." });
    }
        
    [HttpDelete("Eliminar_DiaInhabil/{fecha}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Eliminar_DiaInhabil(DateTime fecha)
    {
        var fechaNormalizada = fecha.Date;

        var diaInhabil = await _context.DiaInhabilManual
            .FirstOrDefaultAsync(d => d.Fecha == fechaNormalizada);

        if (diaInhabil == null)
        {
            return NotFound();
        }

        _context.DiaInhabilManual.Remove(diaInhabil);
        await _context.SaveChangesAsync();

        await RecalcularFechasLimiteEnSolicitudes();

        return NoContent();
    }

    private async Task RecalcularFechasLimiteEnSolicitudes()
    {
        var diasInhabiles = await _context.DiaInhabilManual
            .Select(d => d.Fecha.Date)
            .ToListAsync();

        var expedientes = await _context.Expedientes
            .Where(e => e.FechaInicio != null)
            .ToListAsync();

        foreach (var expediente in expedientes)
        {
            int diasHabiles = expediente.TipoSolicitud == "DAI" ? 10 : 20;

            if (expediente.Ampliacion != null && expediente.Ampliacion.ToUpper() == "SI")
            {
                diasHabiles *= 2; 
            }

            var fechaInicio =expediente.FechaInicio!.Value.Date;
            var fechaCalculada = fechaInicio;
            // int contados = 0;

            while (diasHabiles > 0)
            {
                fechaCalculada = fechaCalculada.AddDays(1);

                bool esFinDeSemana = fechaCalculada.DayOfWeek == DayOfWeek.Saturday || fechaCalculada.DayOfWeek == DayOfWeek.Sunday;
                bool esDiaInhabil = diasInhabiles.Contains(fechaCalculada.Date);

                if (!esFinDeSemana && !esDiaInhabil)
                {
                    diasHabiles--;
                }
            }

            var fechalimiteConHora = fechaCalculada.AddHours(23).AddMinutes(59).AddSeconds(59);

            if (expediente.TipoSolicitud == "DAI")
            {
                expediente.FechaLimiteRespuesta10dias = fechalimiteConHora;
                expediente.FechaLimiteRespuesta20dias = null;
            }
            else if (expediente.TipoSolicitud == "ARCO")
            {
                expediente.FechaLimiteRespuesta20dias = fechalimiteConHora;
                expediente.FechaLimiteRespuesta10dias = null;
            }
        }

        await _context.SaveChangesAsync();
    }
}
