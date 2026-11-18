using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;
using Microsoft.Data.SqlClient;
using Dapper;
using SolicitudesAPI;
using SolicitudesShared.RecursosRevision;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using static SolicitudesShared.RecursosRevision.ExpedienteRevisionDTO;
using QuestPDF.Fluent;         // <-- ESTE USING ACTIVA .GeneratePdf()
using QuestPDF.Infrastructure;  // <-- Requerido para interfaces de QuestPDF

namespace SolicitudesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecursoRevisionController : ControllerBase
    {
        private readonly IConfiguration _config;

        public RecursoRevisionController(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection GetConnection() =>
            new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        private sealed class ArchivoPdfDTO
        {
            public string NombreArchivo { get; set; } = string.Empty;
            public byte[] Contenido { get; set; } = [];
        }

        // 1. OBTENER LOS ÚLTIMOS 10 EXPEDIENTES (Para la tabla inferior de la SPA)
        [HttpGet("Expediente/Lista")]
        public async Task<IActionResult> ObtenerTodosLosExpedientes()
        {
            using var con = GetConnection();

            string query = @"
                SELECT TOP 10 r.*, 
                       (SELECT COUNT(1) FROM Expediente_Archivos a WHERE a.id_expediente = r.id_expediente) AS TotalDocumentos
                FROM Expediente_Recurso r
                ORDER BY r.fecha_modificacion DESC";

            var lista = await con.QueryAsync<ExpedienteRevisionDTO>(query);
            return Ok(lista);
        }

        // 2. OBTENER UN EXPEDIENTE POR SU ID EXACTO
        [HttpGet("Expediente/{id}")]
        public async Task<IActionResult> ObtenerExpedientePorId(int id)
        {
            using var con = GetConnection();
            string query = @"
                SELECT r.*, 
                       (SELECT COUNT(1) FROM Expediente_Archivos a WHERE a.id_expediente = r.id_expediente) AS TotalDocumentos
                FROM Expediente_Recurso r
                WHERE r.id_expediente = @id";

            var expediente = await con.QuerySingleOrDefaultAsync<ExpedienteRevisionDTO>(query, new { id });
            if (expediente is not null) return Ok(expediente);

            return NotFound(new { mensaje = "Expediente no encontrado" });
        }

        // 3. GUARDAR NUEVO EXPEDIENTE
        [HttpPost("Expediente")]
        [Authorize(Roles = "ADMINISTRADOR,SUPERVISIÓN,CAPTURA")]
        public async Task<IActionResult> GuardarExpediente([FromBody] ExpedienteRevisionDTO m)
        {
            if (string.IsNullOrWhiteSpace(m.NumeroRecurso))
                return BadRequest(new { mensaje = "El número de recurso es obligatorio." });

            if (string.IsNullOrWhiteSpace(m.FolioSolicitud))
                return BadRequest(new { mensaje = "El folio de la solicitud es obligatorio." });

            using var con = GetConnection();

            // 1. Validar que el Folio exista previamente en el Módulo 1 (Tabla Expedientes)
            string queryExisteFolio = "SELECT COUNT(1) FROM Expedientes WHERE CAST(Folio AS NVARCHAR(50)) = @folio";
            if (await con.ExecuteScalarAsync<int>(queryExisteFolio, new { folio = m.FolioSolicitud.Trim() }) == 0)
            {
                return BadRequest(new { mensaje = $"El folio de solicitud '{m.FolioSolicitud.Trim()}' no está registrado en el Módulo de Solicitudes." });
            }

            // 2. Validar que el Número de Recurso no esté duplicado en el Módulo 2
            string queryFolioUsado = "SELECT COUNT(1) FROM Expediente_Recurso WHERE folio_solicitud = @folio";
            if (await con.ExecuteScalarAsync<int>(queryFolioUsado, new { folio = m.FolioSolicitud.Trim() }) > 0)
            {
                return Conflict(new { mensaje = $"El folio de solicitud '{m.FolioSolicitud.Trim()}' ya se encuentra vinculado a otro recurso de revisión." });
            }

            string query = @"
                INSERT INTO Expediente_Recurso (
                    numero_recurso, folio_solicitud, materia_recurso, nombre_recurrente, fecha_presentacion, estatus, responsable_interno,
                    contenido_solicitud, fecha_respuesta_solicitud, respuesta_otorgada, razon_interposicion,
                    fecha_admision, fecha_notificacion_admision, fecha_limite_contestacion, fecha_contestacion_recurso, sentido_contestacion,
                    fecha_cierre_instruccion, fecha_resolucion, sentido_resolucion, fecha_notificacion_resolucion,
                    requiere_cumplimiento, fecha_limite_cumplimiento, fecha_cumplio, cumplio_resolucion, acuerdo_cumplimiento, observaciones,
                    fecha_creacion, fecha_modificacion
                ) VALUES (
                    @numero_recurso, @folio_solicitud, @materia_recurso, @nombre_recurrente, @fecha_presentacion, @estatus, @responsable_interno,
                    @contenido_solicitud, @fecha_respuesta_solicitud, @respuesta_otorgada, @razon_interposicion,
                    @fecha_admision, @fecha_notificacion_admision, @fecha_limite_contestacion, @fecha_contestacion_recurso, @sentido_contestacion,
                    @fecha_cierre_instruccion, @fecha_resolucion, @sentido_resolucion, @fecha_notificacion_resolucion,
                    @requiere_cumplimiento, @fecha_limite_cumplimiento, @fecha_cumplio, @cumplio_resolucion, @acuerdo_cumplimiento, @observaciones,
                    GETDATE(), GETDATE()
                );
                SELECT SCOPE_IDENTITY();";

            var parameters = new DynamicParameters();
            parameters.Add("@numero_recurso", m.NumeroRecurso?.Trim());
            parameters.Add("@folio_solicitud", m.FolioSolicitud?.Trim());
            parameters.Add("@materia_recurso", m.MateriaRecurso);
            parameters.Add("@nombre_recurrente", m.NombreRecurrente);
            parameters.Add("@fecha_presentacion", m.FechaPresentacion);
            parameters.Add("@estatus", m.Estatus);
            parameters.Add("@responsable_interno", m.ResponsableInterno);
            parameters.Add("@contenido_solicitud", m.ContenidoSolicitud);
            parameters.Add("@fecha_respuesta_solicitud", m.FechaRespuestaSolicitud);
            parameters.Add("@respuesta_otorgada", m.RespuestaOtorgada);
            parameters.Add("@razon_interposicion", m.RazonInterposicion);
            parameters.Add("@fecha_admision", m.FechaAdmision);
            parameters.Add("@fecha_notificacion_admision", m.FechaNotificacionAdmision);
            parameters.Add("@fecha_limite_contestacion", m.FechaLimiteContestacion);
            parameters.Add("@fecha_contestacion_recurso", m.FechaContestacionRecurso);
            parameters.Add("@sentido_contestacion", m.SentidoContestacion);
            parameters.Add("@fecha_cierre_instruccion", m.FechaCierreInstruccion);
            parameters.Add("@fecha_resolucion", m.FechaResolucion);
            parameters.Add("@sentido_resolucion", m.SentidoResolucion);
            parameters.Add("@fecha_notificacion_resolucion", m.FechaNotificacionResolucion);
            parameters.Add("@requiere_cumplimiento", m.RequiereCumplimiento);
            parameters.Add("@fecha_limite_cumplimiento", m.FechaLimiteCumplimiento);
            parameters.Add("@fecha_cumplio", m.FechaCumplio);
            parameters.Add("@cumplio_resolucion", m.CumplioResolucion);
            parameters.Add("@acuerdo_cumplimiento", m.AcuerdoCumplimiento);
            parameters.Add("@observaciones", m.Observaciones);

            var nuevoId = await con.ExecuteScalarAsync<int>(query, parameters);
            return Ok(new { idExpedienteCreado = nuevoId, mensaje = "Expediente guardado exitosamente" });
        }

        // 4. ACTUALIZAR EXPEDIENTE EXISTENTE
        [HttpPut("Expediente/Actualizar")]
        [Authorize(Roles = "ADMINISTRADOR,SUPERVISIÓN,CAPTURA")]
        public async Task<IActionResult> ActualizarExpediente([FromBody] ExpedienteRevisionDTO m)
        {
            if (m.IdExpediente <= 0)
                return BadRequest(new { mensaje = "ID de expediente no válido." });

            if (string.IsNullOrWhiteSpace(m.FolioSolicitud))
                return BadRequest(new { mensaje = "El folio de la solicitud es obligatorio." });

            using var con = GetConnection();

            // Validar que el Folio exista en la tabla Expedientes del Módulo 1
            string queryExisteFolio = "SELECT COUNT(1) FROM Expedientes WHERE CAST(Folio AS NVARCHAR(50)) = @folio";
            if (await con.ExecuteScalarAsync<int>(queryExisteFolio, new { folio = m.FolioSolicitud.Trim() }) == 0)
            {
                return BadRequest(new { mensaje = $"El folio de solicitud '{m.FolioSolicitud.Trim()}' no está registrado en el Módulo de Solicitudes." });
            }

            string queryFolioUsado = "SELECT COUNT(1) FROM Expediente_Recurso WHERE folio_solicitud = @folio AND id_expediente != @id";
            if (await con.ExecuteScalarAsync<int>(queryFolioUsado, new { folio = m.FolioSolicitud.Trim(), id = m.IdExpediente }) > 0)
            {
                return Conflict(new { mensaje = $"El folio de solicitud '{m.FolioSolicitud.Trim()}' ya se encuentra vinculado a otro recurso de revisión." });
            }

            string query = @"
                UPDATE Expediente_Recurso SET
                    numero_recurso = @numero_recurso,
                    folio_solicitud = @folio_solicitud,
                    materia_recurso = @materia_recurso,
                    nombre_recurrente = @nombre_recurrente,
                    fecha_presentacion = @fecha_presentacion,
                    estatus = @estatus,
                    responsable_interno = @responsable_interno,
                    contenido_solicitud = @contenido_solicitud,
                    fecha_respuesta_solicitud = @fecha_respuesta_solicitud,
                    respuesta_otorgada = @respuesta_otorgada,
                    razon_interposicion = @razon_interposicion,
                    fecha_admision = @fecha_admision,
                    fecha_notificacion_admision = @fecha_notificacion_admision,
                    fecha_limite_contestacion = @fecha_limite_contestacion,
                    fecha_contestacion_recurso = @fecha_contestacion_recurso,
                    sentido_contestacion = @sentido_contestacion,
                    fecha_cierre_instruccion = @fecha_cierre_instruccion,
                    fecha_resolucion = @fecha_resolucion,
                    sentido_resolucion = @sentido_resolucion,
                    fecha_notificacion_resolucion = @fecha_notificacion_resolucion,
                    requiere_cumplimiento = @requiere_cumplimiento,
                    fecha_limite_cumplimiento = @fecha_limite_cumplimiento,
                    fecha_cumplio = @fecha_cumplio,
                    cumplio_resolucion = @cumplio_resolucion,
                    acuerdo_cumplimiento = @acuerdo_cumplimiento,
                    observaciones = @observaciones,
                    fecha_modificacion = GETDATE()
                WHERE id_expediente = @id";

            var parameters = new DynamicParameters();
            parameters.Add("@id", m.IdExpediente);
            parameters.Add("@numero_recurso", m.NumeroRecurso?.Trim());
            parameters.Add("@folio_solicitud", m.FolioSolicitud?.Trim());
            parameters.Add("@materia_recurso", m.MateriaRecurso);
            parameters.Add("@nombre_recurrente", m.NombreRecurrente);
            parameters.Add("@fecha_presentacion", m.FechaPresentacion);
            parameters.Add("@estatus", m.Estatus);
            parameters.Add("@responsable_interno", m.ResponsableInterno);
            parameters.Add("@contenido_solicitud", m.ContenidoSolicitud);
            parameters.Add("@fecha_respuesta_solicitud", m.FechaRespuestaSolicitud);
            parameters.Add("@respuesta_otorgada", m.RespuestaOtorgada);
            parameters.Add("@razon_interposicion", m.RazonInterposicion);
            parameters.Add("@fecha_admision", m.FechaAdmision);
            parameters.Add("@fecha_notificacion_admision", m.FechaNotificacionAdmision);
            parameters.Add("@fecha_limite_contestacion", m.FechaLimiteContestacion);
            parameters.Add("@fecha_contestacion_recurso", m.FechaContestacionRecurso);
            parameters.Add("@sentido_contestacion", m.SentidoContestacion);
            parameters.Add("@fecha_cierre_instruccion", m.FechaCierreInstruccion);
            parameters.Add("@fecha_resolucion", m.FechaResolucion);
            parameters.Add("@sentido_resolucion", m.SentidoResolucion);
            parameters.Add("@fecha_notificacion_resolucion", m.FechaNotificacionResolucion);
            parameters.Add("@requiere_cumplimiento", m.RequiereCumplimiento);
            parameters.Add("@fecha_limite_cumplimiento", m.FechaLimiteCumplimiento);
            parameters.Add("@fecha_cumplio", m.FechaCumplio);
            parameters.Add("@cumplio_resolucion", m.CumplioResolucion);
            parameters.Add("@acuerdo_cumplimiento", m.AcuerdoCumplimiento);
            parameters.Add("@observaciones", m.Observaciones);

            await con.ExecuteAsync(query, parameters);

            return Ok(new { mensaje = "Expediente actualizado exitosamente" });
        }

        // 5. BÚSQUEDA REACTIVA DIRECTA EN SQL
        [HttpGet("Expediente/Buscar")]
        public async Task<IActionResult> BuscarExpedientes(
            [FromQuery] string? criterio,
            [FromQuery] string? estatus,
            [FromQuery] string? materia,
            [FromQuery] string? resolucion,
            [FromQuery] int limite = 30)
        {
            using var con = GetConnection();

            string query = @"
                SELECT TOP (@limite) r.*,
                       (SELECT COUNT(1) FROM Expediente_Archivos a WHERE a.id_expediente = r.id_expediente) AS TotalDocumentos
                FROM Expediente_Recurso r
                WHERE 1 = 1 ";

            if (!string.IsNullOrWhiteSpace(criterio))
            {
                query += " AND (r.folio_solicitud LIKE '%' + @criterio + '%' OR r.numero_recurso LIKE '%' + @criterio + '%' OR r.nombre_recurrente LIKE '%' + @criterio + '%') ";
            }
            if (!string.IsNullOrWhiteSpace(estatus))
            {
                query += " AND r.estatus = @estatus ";
            }
            if (!string.IsNullOrWhiteSpace(materia))
            {
                query += " AND r.materia_recurso LIKE '%' + @materia + '%' ";
            }
            if (!string.IsNullOrWhiteSpace(resolucion))
            {
                query += " AND r.sentido_resolucion = @resolucion ";
            }

            query += " ORDER BY r.fecha_modificacion DESC";

            var parameters = new DynamicParameters();
            parameters.Add("@limite", limite);
            if (!string.IsNullOrWhiteSpace(criterio)) parameters.Add("@criterio", criterio.Trim());
            if (!string.IsNullOrWhiteSpace(estatus)) parameters.Add("@estatus", estatus.Trim());
            if (!string.IsNullOrWhiteSpace(materia)) parameters.Add("@materia", materia.Trim());
            if (!string.IsNullOrWhiteSpace(resolucion)) parameters.Add("@resolucion", resolucion.Trim());

            var lista = await con.QueryAsync<ExpedienteRevisionDTO>(query, parameters);
            return Ok(lista);
        }

        // 6. SUBIR PDF CON METADATOS
        [HttpPost("Expediente/SubirPDF")]
        [Authorize(Roles = "ADMINISTRADOR,SUPERVISIÓN,CAPTURA")]
        public async Task<IActionResult> SubirPDF(
            [FromForm] int idExpediente,
            [FromForm] string tipoDocumento,
            [FromForm] DateTime fechaDocumento,
            [FromForm] string? descripcion,
            [FromForm] string? usuario,
            [FromForm] IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);

            using var con = GetConnection();
            string query = @"
                INSERT INTO Expediente_Archivos
                    (id_expediente, nombre_archivo, contenido, tipo_documento, fecha_documento, descripcion, usuario_registro, fecha_registro)
                VALUES 
                    (@id_expediente, @nombre_archivo, @contenido, @tipo_documento, @fecha_documento, @descripcion, @usuario_registro, GETDATE());
                
                UPDATE Expediente_Recurso SET fecha_modificacion = GETDATE() WHERE id_expediente = @id_expediente;";



            await con.ExecuteAsync(query, new
            {
                id_expediente = idExpediente,
                nombre_archivo = archivo.FileName,
                contenido = ms.ToArray(),
                tipo_documento = tipoDocumento ?? "Documento",
                fecha_documento = fechaDocumento,
                descripcion = descripcion ?? string.Empty,
                usuario_registro = User.Identity?.Name ?? "Usuario"
            });

            return Ok(new { mensaje = "Documento guardado correctamente" });
        }

        // 7. OBTENER LISTA DE PDFs DEL EXPEDIENTE
        [HttpGet("Expediente/{idExpediente}/PDFs")]
        public async Task<IActionResult> ObtenerPDFs(int idExpediente)
        {
            using var con = GetConnection();
            string query = @"
                SELECT id_archivo AS IdArchivo, 
                       id_expediente AS IdExpediente, 
                       nombre_archivo AS NombreArchivo,
                       ISNULL(tipo_documento, 'Documento') AS tipo_documento,
                       ISNULL(fecha_documento, fecha_registro) AS fecha_documento,
                       ISNULL(descripcion, '') AS descripcion,
                       ISNULL(usuario_registro, 'Usuario') AS Usuario
                FROM Expediente_Archivos
                WHERE id_expediente = @id
                ORDER BY id_archivo DESC";

            var lista = await con.QueryAsync<ArchivoExpedienteDTO>(query, new { id = idExpediente });
            return Ok(lista);
        }

        // 8. OBTENER BINARIO DEL PDF (Para Ver inline o Descargar)
        [HttpGet("Expediente/PDF/{idArchivo}")]
        public async Task<IActionResult> DescargarPDF(int idArchivo)
        {
            using var con = GetConnection();
            const string query = "SELECT nombre_archivo AS NombreArchivo, contenido AS Contenido FROM Expediente_Archivos WHERE id_archivo = @id";
            var archivo = await con.QuerySingleOrDefaultAsync<ArchivoPdfDTO>(query, new { id = idArchivo });
            if (archivo is not null)
            {
                var disposition = new ContentDispositionHeaderValue("inline");
                disposition.SetHttpFileName(archivo.NombreArchivo);
                Response.Headers.ContentDisposition = disposition.ToString();

                return File(archivo.Contenido, "application/pdf");
            }

            return NotFound();
        }

        [HttpDelete("Expediente/PDF/{idArchivo}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> EliminarPDF(int idArchivo)
        {
            using var con = GetConnection();
            string query = "DELETE FROM Expediente_Archivos WHERE id_archivo = @id";
            int filasAfectadas = await con.ExecuteAsync(query, new { id = idArchivo });
            if (filasAfectadas > 0) return Ok(new { mensaje = "Documento eliminado correctamente" });
            return NotFound("Archivo no encontrado");
        }

        [HttpDelete("Expediente/{idExpediente}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> EliminarExpediente(int idExpediente)
        {
            using var con = GetConnection();
            string query = @"
                DELETE FROM Expediente_Archivos WHERE id_expediente = @id;
                DELETE FROM Expediente_Bitacora WHERE id_expediente = @id;
                DELETE FROM Expediente_Recurso WHERE id_expediente = @id;";
            int filasAfectadas = await con.ExecuteAsync(query, new { id = idExpediente });
            if (filasAfectadas > 0) return Ok(new { mensaje = "Expediente eliminado exitosamente" });
            return NotFound("Expediente no encontrado");
        }

        // ENDPOINT PARA GENERAR FICHA TÉCNICA PDF (Usando QuestPDF)
        [HttpPost("Expediente/PDF")]
        public IActionResult GenerarPDF([FromBody] ExpedienteRevisionDTO modelo)
        {
            try
            {
                var documento = new ExpedienteRevisionPDF(modelo);

                // QuestPDF genera los bytes directamente
                byte[] pdfBytes = documento.GeneratePdf();

                return File(pdfBytes, "application/pdf", "ExpedienteRevision.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error generando PDF", detail = ex.Message });
            }
        }

        // OBTENER BITÁCORA DEL EXPEDIENTE
        [HttpGet("Expediente/{idExpediente}/Bitacora")]
        public async Task<IActionResult> ObtenerBitacora(int idExpediente)
        {
            using var con = GetConnection();

            string query = @"
                SELECT id_actuacion AS IdActuacion, 
                       id_expediente AS IdExpediente,
                       fecha_actuacion AS FechaActuacion, 
                       actuacion AS Actuacion,
                       descripcion AS Descripcion, 
                       ISNULL(documento_vinculado, N'—') AS DocumentoVinculado,
                       usuario AS Usuario, 
                       fecha_registro AS FechaRegistro
                FROM Expediente_Bitacora
                WHERE id_expediente = @id
                ORDER BY fecha_actuacion ASC, id_actuacion ASC";

            var lista = await con.QueryAsync<BitacoraActuacionDTO>(query, new { id = idExpediente });
            return Ok(lista);
        }

        // AGREGAR NUEVA ACTUACIÓN A LA BITÁCORA
        [HttpPost("Expediente/Bitacora")]
        [Authorize(Roles = "ADMINISTRADOR,SUPERVISIÓN,CAPTURA")]
        public async Task<IActionResult> RegistrarActuacion([FromBody] BitacoraActuacionDTO dto)
        {
            // Solo validamos los campos obligatorios: Expediente, Fecha y Tipo de Actuación
            if (dto.IdExpediente <= 0 || string.IsNullOrWhiteSpace(dto.Actuacion) || dto.FechaActuacion == default)
                return BadRequest("La fecha y el tipo de actuación son obligatorios.");

            using var con = GetConnection();
            string query = @"
                INSERT INTO Expediente_Bitacora
                    (id_expediente, fecha_actuacion, actuacion, descripcion, documento_vinculado, fecha_registro)
                VALUES 
                    (@id_expediente, @fecha_actuacion, @actuacion, @descripcion, @documento_vinculado, GETDATE());";

            await con.ExecuteAsync(query, new
            {
                id_expediente = dto.IdExpediente,
                fecha_actuacion = dto.FechaActuacion,
                actuacion = dto.Actuacion.Trim(),
                descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? string.Empty : dto.Descripcion.Trim(),
                documento_vinculado = string.IsNullOrWhiteSpace(dto.DocumentoVinculado) ? null : dto.DocumentoVinculado.Trim()
            });

            return Ok(new { mensaje = "Actuación registrada con éxito" });
        }

        // 9. OBTENER LISTA DE FOLIOS DISPONIBLES EN EL MÓDULO 1 (Excluyendo los ya usados en Módulo 2)
        [HttpGet("Expedientes/Folios")]
        public async Task<IActionResult> ObtenerFoliosDisponibles()
        {
            using var con = GetConnection();

            // Seleccionamos el Folio de Expedientes, excluyendo aquellos que ya están en Expediente_Recurso
            string query = @"
                SELECT ID as Id, 
                       CAST(Folio AS NVARCHAR(50)) AS Folio, 
                       NombreSolicitante
                FROM Expedientes
                WHERE Folio IS NOT NULL
                  AND CAST(Folio AS NVARCHAR(50)) NOT IN (
                      SELECT folio_solicitud 
                      FROM Expediente_Recurso 
                      WHERE folio_solicitud IS NOT NULL
                  )
                ORDER BY ID DESC";

            var folios = await con.QueryAsync(query);
            return Ok(folios);
        }

        // OBTENER DATOS ESTADÍSTICOS CONSOLIDADOS
        [HttpGet("Estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin,
            [FromQuery] int? anio,
            [FromQuery] string? materia)
        {
            try
            {
                using var con = GetConnection();
                // Construcción dinámica de condiciones WHERE
                var condiciones = new List<string> { "1 = 1" };

                // Validar rango de fechas (usando fecha_presentacion como pivote)
                if (fechaInicio.HasValue && fechaInicio.Value.Year >= 1900)
                {
                    condiciones.Add("fecha_presentacion >= @FechaInicio");
                }

                if (fechaFin.HasValue && fechaFin.Value.Year >= 1900)
                {
                    condiciones.Add("fecha_presentacion <= @FechaFin");
                }

                // Mantener los filtros anteriores como respaldo
                if (anio.HasValue && anio.Value > 0)
                    condiciones.Add(@"(YEAR(COALESCE(fecha_presentacion, fecha_notificacion_admision, fecha_resolucion, fecha_creacion)) = @Anio 
                           OR numero_recurso LIKE '%/' + CAST(@Anio AS VARCHAR))");

                if (!string.IsNullOrWhiteSpace(materia))
                    condiciones.Add("materia_recurso LIKE '%' + @Materia + '%'");

                string whereClause = string.Join(" AND ", condiciones);

                string query = $@"
                    -- Total Registrados
                    SELECT 
                        'Recursos registrados' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause}

                    UNION ALL

                    -- En Trámite
                    SELECT 
                        'Recursos en trámite' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND (estatus LIKE '%Trámite%' OR estatus LIKE '%Tramite%')

                    UNION ALL

                    -- Concluidos
                    SELECT 
                        'Recursos concluidos' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND estatus LIKE '%Conclu%'

                    UNION ALL

                    -- Admitidos
                    SELECT 
                        'Recursos admitidos' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND estatus LIKE '%Admitido%'

                    UNION ALL

                    -- En Cumplimiento
                    SELECT 
                        'Recursos en cumplimiento' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND (estatus LIKE '%Cumplimiento%' OR requiere_cumplimiento = 1)

                    UNION ALL

                    -- Sentido: Confirma
                    SELECT 
                        'Sentido: Confirma' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND sentido_resolucion LIKE '%Confirma%'

                    UNION ALL

                    -- Sentido: Revoca
                    SELECT 
                        'Sentido: Revoca' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND sentido_resolucion LIKE '%Revoca%'

                    UNION ALL

                    -- Sentido: Modifica
                    SELECT 
                        'Sentido: Modifica' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND sentido_resolucion LIKE '%Modifica%'

                    UNION ALL

                    -- Sentido: Sobresee
                    SELECT 
                        'Sentido: Sobresee' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(numero_recurso, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM Expediente_Recurso
                    WHERE {whereClause} AND sentido_resolucion LIKE '%Sobresee%'
                ";

                var parameters = new DynamicParameters();
                if (fechaInicio.HasValue && fechaInicio.Value.Year >= 1900)
                    parameters.Add("@FechaInicio", fechaInicio.Value);

                if (fechaFin.HasValue && fechaFin.Value.Year >= 1900)
                    parameters.Add("@FechaFin", fechaFin.Value);

                if (anio.HasValue && anio.Value > 0) parameters.Add("@Anio", anio.Value);
                if (!string.IsNullOrWhiteSpace(materia)) parameters.Add("@Materia", materia.Trim());

                var estadisticas = await con.QueryAsync<EstadisticaDTO>(query, parameters);
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                // Devuelve el mensaje interno preservando las cabeceras HTTP hacia el cliente
                return StatusCode(500, new { error = "Fallo en estadísticas", detalle = ex.Message });
            }
        }
    }
}