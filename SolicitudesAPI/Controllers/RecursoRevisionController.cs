using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
                       (SELECT COUNT(1) FROM EXPEDIENTE_ARCHIVOS a WHERE a.ID_EXPEDIENTE = r.ID_EXPEDIENTE) AS TotalDocumentos
                FROM EXPEDIENTE_RECURSO r
                ORDER BY r.FECHA_MODIFICACION DESC";

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
                       (SELECT COUNT(1) FROM EXPEDIENTE_ARCHIVOS a WHERE a.ID_EXPEDIENTE = r.ID_EXPEDIENTE) AS TotalDocumentos
                FROM EXPEDIENTE_RECURSO r
                WHERE r.ID_EXPEDIENTE = @ID";

            var expediente = await con.QuerySingleOrDefaultAsync<ExpedienteRevisionDTO>(query, new { ID = id });
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

            using var con = GetConnection();

            string queryExiste = "SELECT COUNT(1) FROM EXPEDIENTE_RECURSO WHERE NUMERO_RECURSO = @NUMERO";
            if (await con.ExecuteScalarAsync<int>(queryExiste, new { NUMERO = m.NumeroRecurso.Trim() }) > 0)
            {
                return Conflict(new { mensaje = $"El recurso '{m.NumeroRecurso}' ya existe en el sistema." });
            }

            string query = @"
                INSERT INTO EXPEDIENTE_RECURSO (
                    NUMERO_RECURSO, FOLIO_SOLICITUD, MATERIA_RECURSO, NOMBRE_RECURRENTE, FECHA_PRESENTACION, ESTATUS, RESPONSABLE_INTERNO,
                    CONTENIDO_SOLICITUD, FECHA_RESPUESTA_SOLICITUD, RESPUESTA_OTORGADA, RAZON_INTERPOSICION,
                    FECHA_ADMISION, FECHA_NOTIFICACION_ADMISION, FECHA_LIMITE_CONTESTACION, FECHA_CONTESTACION_RECURSO, SENTIDO_CONTESTACION,
                    FECHA_CIERRE_INSTRUCCION, FECHA_RESOLUCION, SENTIDO_RESOLUCION, FECHA_NOTIFICACION_RESOLUCION,
                    REQUIERE_CUMPLIMIENTO, FECHA_LIMITE_CUMPLIMIENTO, FECHA_CUMPLIO, CUMPLIO_RESOLUCION, ACUERDO_CUMPLIMIENTO, OBSERVACIONES,
                    FECHA_CREACION, FECHA_MODIFICACION
                ) VALUES (
                    @NUMERO_RECURSO, @FOLIO_SOLICITUD, @MATERIA_RECURSO, @NOMBRE_RECURRENTE, @FECHA_PRESENTACION, @ESTATUS, @RESPONSABLE_INTERNO,
                    @CONTENIDO_SOLICITUD, @FECHA_RESPUESTA_SOLICITUD, @RESPUESTA_OTORGADA, @RAZON_INTERPOSICION,
                    @FECHA_ADMISION, @FECHA_NOTIFICACION_ADMISION, @FECHA_LIMITE_CONTESTACION, @FECHA_CONTESTACION_RECURSO, @SENTIDO_CONTESTACION,
                    @FECHA_CIERRE_INSTRUCCION, @FECHA_RESOLUCION, @SENTIDO_RESOLUCION, @FECHA_NOTIFICACION_RESOLUCION,
                    @REQUIERE_CUMPLIMIENTO, @FECHA_LIMITE_CUMPLIMIENTO, @FECHA_CUMPLIO, @CUMPLIO_RESOLUCION, @ACUERDO_CUMPLIMIENTO, @OBSERVACIONES,
                    GETDATE(), GETDATE()
                );
                SELECT SCOPE_IDENTITY();";

            var parameters = new DynamicParameters();
            parameters.Add("@NUMERO_RECURSO", m.NumeroRecurso?.Trim());
            parameters.Add("@FOLIO_SOLICITUD", m.FolioSolicitud?.Trim());
            parameters.Add("@MATERIA_RECURSO", m.MateriaRecurso);
            parameters.Add("@NOMBRE_RECURRENTE", m.NombreRecurrente);
            parameters.Add("@FECHA_PRESENTACION", m.FechaPresentacion);
            parameters.Add("@ESTATUS", m.Estatus);
            parameters.Add("@RESPONSABLE_INTERNO", m.ResponsableInterno);
            parameters.Add("@CONTENIDO_SOLICITUD", m.ContenidoSolicitud);
            parameters.Add("@FECHA_RESPUESTA_SOLICITUD", m.FechaRespuestaSolicitud);
            parameters.Add("@RESPUESTA_OTORGADA", m.RespuestaOtorgada);
            parameters.Add("@RAZON_INTERPOSICION", m.RazonInterposicion);
            parameters.Add("@FECHA_ADMISION", m.FechaAdmision);
            parameters.Add("@FECHA_NOTIFICACION_ADMISION", m.FechaNotificacionAdmision);
            parameters.Add("@FECHA_LIMITE_CONTESTACION", m.FechaLimiteContestacion);
            parameters.Add("@FECHA_CONTESTACION_RECURSO", m.FechaContestacionRecurso);
            parameters.Add("@SENTIDO_CONTESTACION", m.SentidoContestacion);
            parameters.Add("@FECHA_CIERRE_INSTRUCCION", m.FechaCierreInstruccion);
            parameters.Add("@FECHA_RESOLUCION", m.FechaResolucion);
            parameters.Add("@SENTIDO_RESOLUCION", m.SentidoResolucion);
            parameters.Add("@FECHA_NOTIFICACION_RESOLUCION", m.FechaNotificacionResolucion);
            parameters.Add("@REQUIERE_CUMPLIMIENTO", m.RequiereCumplimiento);
            parameters.Add("@FECHA_LIMITE_CUMPLIMIENTO", m.FechaLimiteCumplimiento);
            parameters.Add("@FECHA_CUMPLIO", m.FechaCumplio);
            parameters.Add("@CUMPLIO_RESOLUCION", m.CumplioResolucion);
            parameters.Add("@ACUERDO_CUMPLIMIENTO", m.AcuerdoCumplimiento);
            parameters.Add("@OBSERVACIONES", m.Observaciones);

            var nuevoId = await con.ExecuteScalarAsync<int>(query, parameters);
            return Ok(new { idExpedienteCreado = nuevoId, mensaje = "Expediente guardado exitosamente" });
        }

        // 4. ACTUALIZAR EXPEDIENTE EXISTENTE
        [HttpPut("Expediente/Actualizar")]
        [Authorize(Roles = "ADMINISTRADOR,SUPERVISIÓN,CAPTURA")]
        public async Task<IActionResult> ActualizarExpediente([FromBody] ExpedienteRevisionDTO m)
        {
            if (m.IdExpediente <= 0)
                return BadRequest("ID de expediente no válido");

            using var con = GetConnection();
            string query = @"
                UPDATE EXPEDIENTE_RECURSO SET
                    NUMERO_RECURSO = @NUMERO_RECURSO,
                    FOLIO_SOLICITUD = @FOLIO_SOLICITUD,
                    MATERIA_RECURSO = @MATERIA_RECURSO,
                    NOMBRE_RECURRENTE = @NOMBRE_RECURRENTE,
                    FECHA_PRESENTACION = @FECHA_PRESENTACION,
                    ESTATUS = @ESTATUS,
                    RESPONSABLE_INTERNO = @RESPONSABLE_INTERNO,
                    CONTENIDO_SOLICITUD = @CONTENIDO_SOLICITUD,
                    FECHA_RESPUESTA_SOLICITUD = @FECHA_RESPUESTA_SOLICITUD,
                    RESPUESTA_OTORGADA = @RESPUESTA_OTORGADA,
                    RAZON_INTERPOSICION = @RAZON_INTERPOSICION,
                    FECHA_ADMISION = @FECHA_ADMISION,
                    FECHA_NOTIFICACION_ADMISION = @FECHA_NOTIFICACION_ADMISION,
                    FECHA_LIMITE_CONTESTACION = @FECHA_LIMITE_CONTESTACION,
                    FECHA_CONTESTACION_RECURSO = @FECHA_CONTESTACION_RECURSO,
                    SENTIDO_CONTESTACION = @SENTIDO_CONTESTACION,
                    FECHA_CIERRE_INSTRUCCION = @FECHA_CIERRE_INSTRUCCION,
                    FECHA_RESOLUCION = @FECHA_RESOLUCION,
                    SENTIDO_RESOLUCION = @SENTIDO_RESOLUCION,
                    FECHA_NOTIFICACION_RESOLUCION = @FECHA_NOTIFICACION_RESOLUCION,
                    REQUIERE_CUMPLIMIENTO = @REQUIERE_CUMPLIMIENTO,
                    FECHA_LIMITE_CUMPLIMIENTO = @FECHA_LIMITE_CUMPLIMIENTO,
                    FECHA_CUMPLIO = @FECHA_CUMPLIO,
                    CUMPLIO_RESOLUCION = @CUMPLIO_RESOLUCION,
                    ACUERDO_CUMPLIMIENTO = @ACUERDO_CUMPLIMIENTO,
                    OBSERVACIONES = @OBSERVACIONES,
                    FECHA_MODIFICACION = GETDATE()
                WHERE ID_EXPEDIENTE = @ID";

            var parameters = new DynamicParameters();
            parameters.Add("@ID", m.IdExpediente);
            parameters.Add("@NUMERO_RECURSO", m.NumeroRecurso?.Trim());
            parameters.Add("@FOLIO_SOLICITUD", m.FolioSolicitud?.Trim());
            parameters.Add("@MATERIA_RECURSO", m.MateriaRecurso);
            parameters.Add("@NOMBRE_RECURRENTE", m.NombreRecurrente);
            parameters.Add("@FECHA_PRESENTACION", m.FechaPresentacion);
            parameters.Add("@ESTATUS", m.Estatus);
            parameters.Add("@RESPONSABLE_INTERNO", m.ResponsableInterno);
            parameters.Add("@CONTENIDO_SOLICITUD", m.ContenidoSolicitud);
            parameters.Add("@FECHA_RESPUESTA_SOLICITUD", m.FechaRespuestaSolicitud);
            parameters.Add("@RESPUESTA_OTORGADA", m.RespuestaOtorgada);
            parameters.Add("@RAZON_INTERPOSICION", m.RazonInterposicion);
            parameters.Add("@FECHA_ADMISION", m.FechaAdmision);
            parameters.Add("@FECHA_NOTIFICACION_ADMISION", m.FechaNotificacionAdmision);
            parameters.Add("@FECHA_LIMITE_CONTESTACION", m.FechaLimiteContestacion);
            parameters.Add("@FECHA_CONTESTACION_RECURSO", m.FechaContestacionRecurso);
            parameters.Add("@SENTIDO_CONTESTACION", m.SentidoContestacion);
            parameters.Add("@FECHA_CIERRE_INSTRUCCION", m.FechaCierreInstruccion);
            parameters.Add("@FECHA_RESOLUCION", m.FechaResolucion);
            parameters.Add("@SENTIDO_RESOLUCION", m.SentidoResolucion);
            parameters.Add("@FECHA_NOTIFICACION_RESOLUCION", m.FechaNotificacionResolucion);
            parameters.Add("@REQUIERE_CUMPLIMIENTO", m.RequiereCumplimiento);
            parameters.Add("@FECHA_LIMITE_CUMPLIMIENTO", m.FechaLimiteCumplimiento);
            parameters.Add("@FECHA_CUMPLIO", m.FechaCumplio);
            parameters.Add("@CUMPLIO_RESOLUCION", m.CumplioResolucion);
            parameters.Add("@ACUERDO_CUMPLIMIENTO", m.AcuerdoCumplimiento);
            parameters.Add("@OBSERVACIONES", m.Observaciones);

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
                SELECT TOP (@Limite) r.*,
                       (SELECT COUNT(1) FROM EXPEDIENTE_ARCHIVOS a WHERE a.ID_EXPEDIENTE = r.ID_EXPEDIENTE) AS TotalDocumentos
                FROM EXPEDIENTE_RECURSO r
                WHERE 1 = 1 ";

            if (!string.IsNullOrWhiteSpace(criterio))
            {
                query += " AND (r.FOLIO_SOLICITUD LIKE '%' + @CRITERIO + '%' OR r.NUMERO_RECURSO LIKE '%' + @CRITERIO + '%' OR r.NOMBRE_RECURRENTE LIKE '%' + @CRITERIO + '%') ";
            }
            if (!string.IsNullOrWhiteSpace(estatus))
            {
                query += " AND r.ESTATUS = @ESTATUS ";
            }
            if (!string.IsNullOrWhiteSpace(materia))
            {
                query += " AND r.MATERIA_RECURSO LIKE '%' + @MATERIA + '%' ";
            }
            if (!string.IsNullOrWhiteSpace(resolucion))
            {
                query += " AND r.SENTIDO_RESOLUCION = @RESOLUCION ";
            }

            query += " ORDER BY r.FECHA_MODIFICACION DESC";

            var parameters = new DynamicParameters();
            parameters.Add("@LIMITE", limite);
            if (!string.IsNullOrWhiteSpace(criterio)) parameters.Add("@CRITERIO", criterio.Trim());
            if (!string.IsNullOrWhiteSpace(estatus)) parameters.Add("@ESTATUS", estatus.Trim());
            if (!string.IsNullOrWhiteSpace(materia)) parameters.Add("@MATERIA", materia.Trim());
            if (!string.IsNullOrWhiteSpace(resolucion)) parameters.Add("@RESOLUCION", resolucion.Trim());
            var lista = await con.QueryAsync<ExpedienteRevisionDTO>(query, parameters);
            return Ok(lista);
        }

        //6. SUBIR PDF CON METADATOS
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
                INSERT INTO EXPEDIENTE_ARCHIVOS
                    (ID_EXPEDIENTE, NOMBRE_ARCHIVO, CONTENIDO, TIPO_DOCUMENTO, FECHA_DOCUMENTO, DESCRIPCION, USUARIO_REGISTRO, FECHA_REGISTRO)
                VALUES 
                    (@IdExpediente, @Nombre, @ContenidoBinario, @Tipo, @FechaDoc, @Descripcion, @Usuario, GETDATE());";

            await con.ExecuteAsync(query, new
            {
                IdExpediente = idExpediente,
                Nombre = archivo.FileName,
                ContenidoBinario = ms.ToArray(),
                Tipo = tipoDocumento ?? "Documento",
                FechaDoc = fechaDocumento,
                Descripcion = descripcion ?? string.Empty,
                Usuario = User.Identity?.Name ?? "Usuario"
            });

            return Ok(new { mensaje = "Documento guardado correctamente" });
        }

        //7. OBTENER LISTA DE PDFs DEL EXPEDIENTE
        [HttpGet("Expediente/{idExpediente}/PDFs")]
        public async Task<IActionResult> ObtenerPDFs(int idExpediente)
        {
            using var con = GetConnection();
            string query = @"
                SELECT ID_ARCHIVO AS IdArchivo, ID_EXPEDIENTE AS IdExpediente, NOMBRE_ARCHIVO AS NombreArchivo,
                       ISNULL(TIPO_DOCUMENTO, 'Documento') AS TIPO_DOCUMENTO,
                       ISNULL(FECHA_DOCUMENTO, FECHA_REGISTRO) AS FECHA_DOCUMENTO,
                       ISNULL(DESCRIPCION, '') AS DESCRIPCION,
                       ISNULL(USUARIO_REGISTRO, 'Usuario') AS Usuario
                FROM EXPEDIENTE_ARCHIVOS
                WHERE ID_EXPEDIENTE = @ID
                ORDER BY ID_ARCHIVO DESC";

            var lista = await con.QueryAsync<ArchivoExpedienteDTO>(query, new { ID = idExpediente });
            return Ok(lista);
        }

        //8. OBTENER BINARIO DEL PDF (Para Ver inline o Descargar)
        [HttpGet("Expediente/PDF/{idArchivo}")]
        public async Task<IActionResult> DescargarPDF(int idArchivo)
        {
            using var con = GetConnection();
            const string query = "SELECT NOMBRE_ARCHIVO AS NombreArchivo, CONTENIDO FROM EXPEDIENTE_ARCHIVOS WHERE ID_ARCHIVO = @ID";
            var archivo = await con.QuerySingleOrDefaultAsync<ArchivoPdfDTO>(query, new { ID = idArchivo });
            if (archivo is not null)
            {
                // FORZAR VISUALIZACIÓN INLINE (En lugar de descarga automática)
                Response.Headers["Content-Disposition"] = $"inline; filename=\"{archivo.NombreArchivo}\"";

                return File(archivo.Contenido, "application/pdf");
            }

            return NotFound();
        }

        [HttpDelete("Expediente/PDF/{idArchivo}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> EliminarPDF(int idArchivo)
        {
            using var con = GetConnection();
            string query = "DELETE FROM EXPEDIENTE_ARCHIVOS WHERE ID_ARCHIVO = @ID";
            int filasAfectadas = await con.ExecuteAsync(query, new { ID = idArchivo });
            if (filasAfectadas > 0) return Ok(new { mensaje = "Documento eliminado correctamente" });
            return NotFound("Archivo no encontrado");
        }

        [HttpDelete("Expediente/{idExpediente}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> EliminarExpediente(int idExpediente)
        {
            using var con = GetConnection();
            string query = @"
                DELETE FROM EXPEDIENTE_ARCHIVOS WHERE ID_EXPEDIENTE = @ID;
                DELETE FROM EXPEDIENTE_RECURSO WHERE ID_EXPEDIENTE = @ID;";
            int filasAfectadas = await con.ExecuteAsync(query, new { ID = idExpediente });
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
                SELECT ID_ACTUACION AS IdActuacion, ID_EXPEDIENTE AS IdExpediente,
                       FECHA_ACTUACION AS FechaActuacion, ACTUACION AS Actuacion,
                       DESCRIPCION AS Descripcion, ISNULL(DOCUMENTO_VINCULADO, N'—') AS DocumentoVinculado,
                       USUARIO AS Usuario, FECHA_REGISTRO AS FechaRegistro
                FROM EXPEDIENTE_BITACORA
                WHERE ID_EXPEDIENTE = @ID
                ORDER BY FECHA_ACTUACION ASC, ID_ACTUACION ASC";

            var lista = await con.QueryAsync<BitacoraActuacionDTO>(query, new { ID = idExpediente });
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
                INSERT INTO EXPEDIENTE_BITACORA
                    (ID_EXPEDIENTE, FECHA_ACTUACION, ACTUACION, DESCRIPCION, DOCUMENTO_VINCULADO, FECHA_REGISTRO)
                VALUES 
                    (@IdExpediente, @FechaActuacion, @Actuacion, @Descripcion, @DocumentoVinculado, GETDATE());";

            await con.ExecuteAsync(query, new
            {
                dto.IdExpediente,
                dto.FechaActuacion,
                Actuacion = dto.Actuacion.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? string.Empty : dto.Descripcion.Trim(),
                DocumentoVinculado = string.IsNullOrWhiteSpace(dto.DocumentoVinculado) ? null : dto.DocumentoVinculado.Trim()
            });

            return Ok(new { mensaje = "Actuación registrada con éxito" });
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

                // Validar rango de fechas (usando FECHA_PRESENTACION como pivote)
                if (fechaInicio.HasValue && fechaInicio.Value.Year >= 1900)
                {
                    condiciones.Add("FECHA_PRESENTACION >= @FechaInicio");
                }

                if (fechaFin.HasValue && fechaFin.Value.Year >= 1900)
                {
                    condiciones.Add("FECHA_PRESENTACION <= @FechaFin");
                }

                    // Mantener los filtros anteriores como respaldo
                    if (anio.HasValue && anio.Value > 0)
                    condiciones.Add(@"(YEAR(COALESCE(FECHA_PRESENTACION, FECHA_NOTIFICACION_ADMISION, FECHA_RESOLUCION, FECHA_CREACION)) = @Anio 
                           OR NUMERO_RECURSO LIKE '%/' + CAST(@Anio AS VARCHAR))");

                if (!string.IsNullOrWhiteSpace(materia))
                    condiciones.Add("MATERIA_RECURSO LIKE '%' + @Materia + '%'");

                string whereClause = string.Join(" AND ", condiciones);

                string query = $@"
                    -- Total Registrados
                    SELECT 
                        'Recursos registrados' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause}

                    UNION ALL

                    -- En Trámite
                    SELECT 
                        'Recursos en trámite' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND (ESTATUS LIKE '%Trámite%' OR ESTATUS LIKE '%Tramite%')

                    UNION ALL

                    -- Concluidos
                    SELECT 
                        'Recursos concluidos' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND ESTATUS LIKE '%Conclu%'

                    UNION ALL

                    -- Admitidos
                    SELECT 
                        'Recursos admitidos' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND ESTATUS LIKE '%Admitido%'

                    UNION ALL

                    -- En Cumplimiento
                    SELECT 
                        'Recursos en cumplimiento' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND (ESTATUS LIKE '%Cumplimiento%' OR REQUIERE_CUMPLIMIENTO = 1)

                    UNION ALL

                    -- Sentido: Confirma
                    SELECT 
                        'Sentido: Confirma' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Confirma%'

                    UNION ALL

                    -- Sentido: Revoca
                    SELECT 
                        'Sentido: Revoca' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Revoca%'

                    UNION ALL

                    -- Sentido: Modifica
                    SELECT 
                        'Sentido: Modifica' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Modifica%'

                    UNION ALL

                    -- Sentido: Sobresee
                    SELECT 
                        'Sentido: Sobresee' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Sobresee%'
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