using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
    public class RecursoRevisionController : ControllerBase
    {
        private readonly IConfiguration _config;

        public RecursoRevisionController(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection GetConnection() =>
            new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        // Helper privado para mapear el DataReader al DTO de forma uniforme
        private static ExpedienteRevisionDTO MapearExpediente(SqlDataReader dr)
        {
            return new ExpedienteRevisionDTO
            {
                IdExpediente = dr.GetInt32(dr.GetOrdinal("ID_EXPEDIENTE")),
                NumeroRecurso = dr["NUMERO_RECURSO"]?.ToString(),
                FolioSolicitud = dr["FOLIO_SOLICITUD"]?.ToString(),
                MateriaRecurso = dr["MATERIA_RECURSO"]?.ToString(),
                NombreRecurrente = dr["NOMBRE_RECURRENTE"]?.ToString(),
                FechaPresentacion = dr["FECHA_PRESENTACION"] as DateTime?,
                Estatus = dr["ESTATUS"]?.ToString(),
                ResponsableInterno = dr["RESPONSABLE_INTERNO"]?.ToString(),

                ContenidoSolicitud = dr["CONTENIDO_SOLICITUD"]?.ToString(),
                FechaRespuestaSolicitud = dr["FECHA_RESPUESTA_SOLICITUD"] as DateTime?,
                RespuestaOtorgada = dr["RESPUESTA_OTORGADA"]?.ToString(),
                RazonInterposicion = dr["RAZON_INTERPOSICION"]?.ToString(),

                FechaAdmision = dr["FECHA_ADMISION"] as DateTime?,
                FechaNotificacionAdmision = dr["FECHA_NOTIFICACION_ADMISION"] as DateTime?,
                FechaLimiteContestacion = dr["FECHA_LIMITE_CONTESTACION"] as DateTime?,
                FechaContestacionRecurso = dr["FECHA_CONTESTACION_RECURSO"] as DateTime?,
                SentidoContestacion = dr["SENTIDO_CONTESTACION"]?.ToString(),
                FechaCierreInstruccion = dr["FECHA_CIERRE_INSTRUCCION"] as DateTime?,
                FechaResolucion = dr["FECHA_RESOLUCION"] as DateTime?,
                SentidoResolucion = dr["SENTIDO_RESOLUCION"]?.ToString(),
                FechaNotificacionResolucion = dr["FECHA_NOTIFICACION_RESOLUCION"] as DateTime?,

                RequiereCumplimiento = dr["REQUIERE_CUMPLIMIENTO"] != DBNull.Value && Convert.ToBoolean(dr["REQUIERE_CUMPLIMIENTO"]),
                FechaLimiteCumplimiento = dr["FECHA_LIMITE_CUMPLIMIENTO"] as DateTime?,
                FechaCumplio = dr["FECHA_CUMPLIO"] as DateTime?,
                CumplioResolucion = dr["CUMPLIO_RESOLUCION"]?.ToString(),
                AcuerdoCumplimiento = dr["ACUERDO_CUMPLIMIENTO"]?.ToString(),
                Observaciones = dr["OBSERVACIONES"]?.ToString(),

                FechaCreacion = dr["FECHA_CREACION"] as DateTime?,
                FechaModificacion = dr["FECHA_MODIFICACION"] as DateTime?,
                TotalDocumentos = dr["TotalDocs"] != DBNull.Value ? Convert.ToInt32(dr["TotalDocs"]) : 0
            };
        }

        // 1. OBTENER LOS ÚLTIMOS 10 EXPEDIENTES (Para la tabla inferior de la SPA)
        [HttpGet("Expediente/Lista")]
        public async Task<IActionResult> ObtenerTodosLosExpedientes()
        {
            var lista = new List<ExpedienteRevisionDTO>();
            using var con = GetConnection();
            await con.OpenAsync();

            string query = @"
                SELECT TOP 10 r.*, 
                       (SELECT COUNT(1) FROM MT_EXPEDIENTE_ARCHIVOS a WHERE a.ID_EXPEDIENTE = r.ID_EXPEDIENTE) AS TotalDocs
                FROM MT_EXPEDIENTE_RECURSO r
                ORDER BY r.FECHA_MODIFICACION DESC";

            using var cmd = new SqlCommand(query, con);
            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                lista.Add(MapearExpediente(dr));
            }

            return Ok(lista);
        }

        // 2. OBTENER UN EXPEDIENTE POR SU ID EXACTO
        [HttpGet("Expediente/{id}")]
        public async Task<IActionResult> ObtenerExpedientePorId(int id)
        {
            using var con = GetConnection();
            string query = @"
                SELECT r.*, 
                       (SELECT COUNT(1) FROM MT_EXPEDIENTE_ARCHIVOS a WHERE a.ID_EXPEDIENTE = r.ID_EXPEDIENTE) AS TotalDocs
                FROM MT_EXPEDIENTE_RECURSO r
                WHERE r.ID_EXPEDIENTE = @ID";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", id);
            await con.OpenAsync();
            using var dr = await cmd.ExecuteReaderAsync();

            if (await dr.ReadAsync())
            {
                return Ok(MapearExpediente(dr));
            }

            return NotFound(new { mensaje = "Expediente no encontrado" });
        }

        // 3. GUARDAR NUEVO EXPEDIENTE
        [HttpPost("Expediente")]
        public async Task<IActionResult> GuardarExpediente([FromBody] ExpedienteRevisionDTO m)
        {
            if (string.IsNullOrWhiteSpace(m.NumeroRecurso))
                return BadRequest(new { mensaje = "El número de recurso es obligatorio." });

            using var con = GetConnection();
            await con.OpenAsync();

            string queryExiste = "SELECT COUNT(1) FROM MT_EXPEDIENTE_RECURSO WHERE NUMERO_RECURSO = @NUMERO";
            using (var cmdExiste = new SqlCommand(queryExiste, con))
            {
                cmdExiste.Parameters.AddWithValue("@NUMERO", m.NumeroRecurso.Trim());
                if (Convert.ToInt32(await cmdExiste.ExecuteScalarAsync()) > 0)
                {
                    return Conflict(new { mensaje = $"El recurso '{m.NumeroRecurso}' ya existe en el sistema." });
                }
            }

            string query = @"
                INSERT INTO MT_EXPEDIENTE_RECURSO (
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

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@NUMERO_RECURSO", (object?)m.NumeroRecurso?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FOLIO_SOLICITUD", (object?)m.FolioSolicitud?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MATERIA_RECURSO", (object?)m.MateriaRecurso ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NOMBRE_RECURRENTE", (object?)m.NombreRecurrente ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_PRESENTACION", (object?)m.FechaPresentacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ESTATUS", (object?)m.Estatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RESPONSABLE_INTERNO", (object?)m.ResponsableInterno ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@CONTENIDO_SOLICITUD", (object?)m.ContenidoSolicitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_RESPUESTA_SOLICITUD", (object?)m.FechaRespuestaSolicitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RESPUESTA_OTORGADA", (object?)m.RespuestaOtorgada ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RAZON_INTERPOSICION", (object?)m.RazonInterposicion ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@FECHA_ADMISION", (object?)m.FechaAdmision ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_NOTIFICACION_ADMISION", (object?)m.FechaNotificacionAdmision ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_LIMITE_CONTESTACION", (object?)m.FechaLimiteContestacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_CONTESTACION_RECURSO", (object?)m.FechaContestacionRecurso ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SENTIDO_CONTESTACION", (object?)m.SentidoContestacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_CIERRE_INSTRUCCION", (object?)m.FechaCierreInstruccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_RESOLUCION", (object?)m.FechaResolucion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SENTIDO_RESOLUCION", (object?)m.SentidoResolucion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_NOTIFICACION_RESOLUCION", (object?)m.FechaNotificacionResolucion ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@REQUIERE_CUMPLIMIENTO", m.RequiereCumplimiento);
            cmd.Parameters.AddWithValue("@FECHA_LIMITE_CUMPLIMIENTO", (object?)m.FechaLimiteCumplimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_CUMPLIO", (object?)m.FechaCumplio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CUMPLIO_RESOLUCION", (object?)m.CumplioResolucion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ACUERDO_CUMPLIMIENTO", (object?)m.AcuerdoCumplimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OBSERVACIONES", (object?)m.Observaciones ?? DBNull.Value);

            var nuevoId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return Ok(new { idExpedienteCreado = nuevoId, mensaje = "Expediente guardado exitosamente" });
        }

        // 4. ACTUALIZAR EXPEDIENTE EXISTENTE
        [HttpPut("Expediente/Actualizar")]
        public async Task<IActionResult> ActualizarExpediente([FromBody] ExpedienteRevisionDTO m)
        {
            if (m.IdExpediente <= 0)
                return BadRequest("ID de expediente no válido");

            using var con = GetConnection();
            string query = @"
                UPDATE MT_EXPEDIENTE_RECURSO SET
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

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", m.IdExpediente);
            cmd.Parameters.AddWithValue("@NUMERO_RECURSO", (object?)m.NumeroRecurso?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FOLIO_SOLICITUD", (object?)m.FolioSolicitud?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MATERIA_RECURSO", (object?)m.MateriaRecurso ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NOMBRE_RECURRENTE", (object?)m.NombreRecurrente ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_PRESENTACION", (object?)m.FechaPresentacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ESTATUS", (object?)m.Estatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RESPONSABLE_INTERNO", (object?)m.ResponsableInterno ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@CONTENIDO_SOLICITUD", (object?)m.ContenidoSolicitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_RESPUESTA_SOLICITUD", (object?)m.FechaRespuestaSolicitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RESPUESTA_OTORGADA", (object?)m.RespuestaOtorgada ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RAZON_INTERPOSICION", (object?)m.RazonInterposicion ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@FECHA_ADMISION", (object?)m.FechaAdmision ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_NOTIFICACION_ADMISION", (object?)m.FechaNotificacionAdmision ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_LIMITE_CONTESTACION", (object?)m.FechaLimiteContestacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_CONTESTACION_RECURSO", (object?)m.FechaContestacionRecurso ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SENTIDO_CONTESTACION", (object?)m.SentidoContestacion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_CIERRE_INSTRUCCION", (object?)m.FechaCierreInstruccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_RESOLUCION", (object?)m.FechaResolucion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SENTIDO_RESOLUCION", (object?)m.SentidoResolucion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_NOTIFICACION_RESOLUCION", (object?)m.FechaNotificacionResolucion ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@REQUIERE_CUMPLIMIENTO", m.RequiereCumplimiento);
            cmd.Parameters.AddWithValue("@FECHA_LIMITE_CUMPLIMIENTO", (object?)m.FechaLimiteCumplimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FECHA_CUMPLIO", (object?)m.FechaCumplio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CUMPLIO_RESOLUCION", (object?)m.CumplioResolucion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ACUERDO_CUMPLIMIENTO", (object?)m.AcuerdoCumplimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OBSERVACIONES", (object?)m.Observaciones ?? DBNull.Value);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

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
            var lista = new List<ExpedienteRevisionDTO>();
            using var con = GetConnection();

            // SE INYECTA EL LÍMITE DIRECTAMENTE EN EL TOP
            string query = $@"
                SELECT TOP ({limite}) r.*, 
                       (SELECT COUNT(1) FROM MT_EXPEDIENTE_ARCHIVOS a WHERE a.ID_EXPEDIENTE = r.ID_EXPEDIENTE) AS TotalDocs
                FROM MT_EXPEDIENTE_RECURSO r
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

            using var cmd = new SqlCommand(query, con);
            if (!string.IsNullOrWhiteSpace(criterio)) cmd.Parameters.AddWithValue("@CRITERIO", criterio.Trim());
            if (!string.IsNullOrWhiteSpace(estatus)) cmd.Parameters.AddWithValue("@ESTATUS", estatus.Trim());
            if (!string.IsNullOrWhiteSpace(materia)) cmd.Parameters.AddWithValue("@MATERIA", materia.Trim());
            if (!string.IsNullOrWhiteSpace(resolucion)) cmd.Parameters.AddWithValue("@RESOLUCION", resolucion.Trim());

            await con.OpenAsync();
            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                lista.Add(MapearExpediente(dr));
            }

            return Ok(lista);
        }

        //6. SUBIR PDF CON METADATOS
        [HttpPost("Expediente/SubirPDF")]
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
            await con.OpenAsync();

            string query = @"
                INSERT INTO MT_EXPEDIENTE_ARCHIVOS 
                    (ID_EXPEDIENTE, NOMBRE_ARCHIVO, CONTENIDO, TIPO_DOCUMENTO, FECHA_DOCUMENTO, DESCRIPCION, USUARIO_REGISTRO, FECHA_REGISTRO)
                VALUES 
                    (@IdExpediente, @Nombre, @ContenidoBinario, @Tipo, @FechaDoc, @Descripcion, @Usuario, GETDATE());";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@IdExpediente", idExpediente);
            cmd.Parameters.AddWithValue("@Nombre", archivo.FileName);
            cmd.Parameters.AddWithValue("@ContenidoBinario", ms.ToArray()); // Coincide con el nuevo parámetro
            cmd.Parameters.AddWithValue("@Tipo", tipoDocumento ?? "Documento");
            cmd.Parameters.AddWithValue("@FechaDoc", fechaDocumento);
            cmd.Parameters.AddWithValue("@Descripcion", descripcion ?? "");
            cmd.Parameters.AddWithValue("@Usuario", string.IsNullOrWhiteSpace(usuario) ? "Usuario" : usuario);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new { mensaje = "Documento guardado correctamente" });
        }

        //7. OBTENER LISTA DE PDFs DEL EXPEDIENTE
        [HttpGet("Expediente/{idExpediente}/PDFs")]
        public async Task<IActionResult> ObtenerPDFs(int idExpediente)
        {
            var lista = new List<ArchivoExpedienteDTO>();
            using var con = GetConnection();
            await con.OpenAsync();

            string query = @"
                SELECT ID_ARCHIVO, ID_EXPEDIENTE, NOMBRE_ARCHIVO, 
                       ISNULL(TIPO_DOCUMENTO, 'Documento') AS TIPO_DOCUMENTO,
                       ISNULL(FECHA_DOCUMENTO, FECHA_REGISTRO) AS FECHA_DOCUMENTO,
                       ISNULL(DESCRIPCION, '') AS DESCRIPCION,
                       ISNULL(USUARIO_REGISTRO, 'Usuario') AS USUARIO_REGISTRO
                FROM MT_EXPEDIENTE_ARCHIVOS
                WHERE ID_EXPEDIENTE = @ID
                ORDER BY ID_ARCHIVO DESC";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", idExpediente);

            using var dr = await cmd.ExecuteReaderAsync();
            while (await dr.ReadAsync())
            {
                lista.Add(new ArchivoExpedienteDTO
                {
                    IdArchivo = dr.GetInt32(0),
                    IdExpediente = dr.GetInt32(1),
                    NombreArchivo = dr.GetString(2),
                    TipoDocumento = dr.GetString(3),
                    FechaDocumento = dr.GetDateTime(4),
                    Descripcion = dr.GetString(5),
                    Usuario = dr.GetString(6)
                });
            }

            return Ok(lista);
        }

        //8. OBTENER BINARIO DEL PDF (Para Ver inline o Descargar)
        [HttpGet("Expediente/PDF/{idArchivo}")]
        public async Task<IActionResult> DescargarPDF(int idArchivo)
        {
            using var con = GetConnection();
            await con.OpenAsync();

            string query = "SELECT NOMBRE_ARCHIVO, CONTENIDO FROM MT_EXPEDIENTE_ARCHIVOS WHERE ID_ARCHIVO = @ID";
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", idArchivo);

            using var dr = await cmd.ExecuteReaderAsync();
            if (await dr.ReadAsync())
            {
                string nombre = dr.GetString(0);
                byte[] data = (byte[])dr["CONTENIDO"];

                // FORZAR VISUALIZACIÓN INLINE (En lugar de descarga automática)
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{nombre}\"");

                return File(data, "application/pdf");
            }

            return NotFound();
        }

        [HttpDelete("Expediente/PDF/{idArchivo}")]
        public async Task<IActionResult> EliminarPDF(int idArchivo)
        {
            using var con = GetConnection();
            string query = "DELETE FROM MT_EXPEDIENTE_ARCHIVOS WHERE ID_ARCHIVO = @ID";
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", idArchivo);
            await con.OpenAsync();
            int filasAfectadas = await cmd.ExecuteNonQueryAsync();
            if (filasAfectadas > 0) return Ok(new { mensaje = "Documento eliminado correctamente" });
            return NotFound("Archivo no encontrado");
        }

        [HttpDelete("Expediente/{idExpediente}")]
        public async Task<IActionResult> EliminarExpediente(int idExpediente)
        {
            using var con = GetConnection();
            string query = @"
                DELETE FROM MT_EXPEDIENTE_ARCHIVOS WHERE ID_EXPEDIENTE = @ID;
                DELETE FROM MT_EXPEDIENTE_RECURSO WHERE ID_EXPEDIENTE = @ID;";
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", idExpediente);
            await con.OpenAsync();
            int filasAfectadas = await cmd.ExecuteNonQueryAsync();
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
            var lista = new List<BitacoraActuacionDTO>();
            using var con = GetConnection();
            await con.OpenAsync();

            string query = @"
                SELECT ID_ACTUACION, ID_EXPEDIENTE, FECHA_ACTUACION, ACTUACION, 
                       DESCRIPCION, DOCUMENTO_VINCULADO, USUARIO, FECHA_REGISTRO
                FROM MT_EXPEDIENTE_BITACORA
                WHERE ID_EXPEDIENTE = @ID
                ORDER BY FECHA_ACTUACION ASC, ID_ACTUACION ASC";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", idExpediente);

            using var dr = await cmd.ExecuteReaderAsync();
            while (await dr.ReadAsync())
            {
                lista.Add(new BitacoraActuacionDTO
                {
                    IdActuacion = dr.GetInt32(0),
                    IdExpediente = dr.GetInt32(1),
                    FechaActuacion = dr.GetDateTime(2),
                    Actuacion = dr.GetString(3),
                    Descripcion = dr.GetString(4),
                    DocumentoVinculado = dr["DOCUMENTO_VINCULADO"] != DBNull.Value ? dr["DOCUMENTO_VINCULADO"].ToString() : "—",
                    Usuario = dr.GetString(6),
                    FechaRegistro = dr.GetDateTime(7)
                });
            }

            return Ok(lista);
        }

        // AGREGAR NUEVA ACTUACIÓN A LA BITÁCORA
        [HttpPost("Expediente/Bitacora")]
        public async Task<IActionResult> RegistrarActuacion([FromBody] BitacoraActuacionDTO dto)
        {
            // Solo validamos los campos obligatorios: Expediente, Fecha y Tipo de Actuación
            if (dto.IdExpediente <= 0 || string.IsNullOrWhiteSpace(dto.Actuacion) || dto.FechaActuacion == default)
                return BadRequest("La fecha y el tipo de actuación son obligatorios.");

            using var con = GetConnection();
            await con.OpenAsync();

            string query = @"
                INSERT INTO MT_EXPEDIENTE_BITACORA 
                    (ID_EXPEDIENTE, FECHA_ACTUACION, ACTUACION, DESCRIPCION, DOCUMENTO_VINCULADO, FECHA_REGISTRO)
                VALUES 
                    (@IdExpediente, @FechaActuacion, @Actuacion, @Descripcion, @DocumentoVinculado, GETDATE());";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@IdExpediente", dto.IdExpediente);
            cmd.Parameters.AddWithValue("@FechaActuacion", dto.FechaActuacion);
            cmd.Parameters.AddWithValue("@Actuacion", dto.Actuacion.Trim());
            cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(dto.Descripcion) ? string.Empty : dto.Descripcion.Trim());
            cmd.Parameters.AddWithValue("@DocumentoVinculado", string.IsNullOrWhiteSpace(dto.DocumentoVinculado) ? (object)DBNull.Value : dto.DocumentoVinculado.Trim());

            await cmd.ExecuteNonQueryAsync();

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
                var estadisticas = new List<EstadisticaDTO>();

                using var con = GetConnection();
                await con.OpenAsync();

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
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause}

                    UNION ALL

                    -- En Trámite
                    SELECT 
                        'Recursos en trámite' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND (ESTATUS LIKE '%Trámite%' OR ESTATUS LIKE '%Tramite%')

                    UNION ALL

                    -- Concluidos
                    SELECT 
                        'Recursos concluidos' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND ESTATUS LIKE '%Conclu%'

                    UNION ALL

                    -- Admitidos
                    SELECT 
                        'Recursos admitidos' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND ESTATUS LIKE '%Admitido%'

                    UNION ALL

                    -- En Cumplimiento
                    SELECT 
                        'Recursos en cumplimiento' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND (ESTATUS LIKE '%Cumplimiento%' OR REQUIERE_CUMPLIMIENTO = 1)

                    UNION ALL

                    -- Sentido: Confirma
                    SELECT 
                        'Sentido: Confirma' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Confirma%'

                    UNION ALL

                    -- Sentido: Revoca
                    SELECT 
                        'Sentido: Revoca' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Revoca%'

                    UNION ALL

                    -- Sentido: Modifica
                    SELECT 
                        'Sentido: Modifica' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Modifica%'

                    UNION ALL

                    -- Sentido: Sobresee
                    SELECT 
                        'Sentido: Sobresee' AS Concepto,
                        COUNT(1) AS Cantidad,
                        ISNULL(STRING_AGG(CAST('(' + ISNULL(NUMERO_RECURSO, 'S/N') + ')' AS NVARCHAR(MAX)), ' '), '') AS Recursos
                    FROM MT_EXPEDIENTE_RECURSO
                    WHERE {whereClause} AND SENTIDO_RESOLUCION LIKE '%Sobresee%'
                ";

                using var cmd = new SqlCommand(query, con);

                if (fechaInicio.HasValue && fechaInicio.Value.Year >= 1900)
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);

                if (fechaFin.HasValue && fechaFin.Value.Year >= 1900)
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Value);

                if (anio.HasValue && anio.Value > 0) cmd.Parameters.AddWithValue("@Anio", anio.Value);
                if (!string.IsNullOrWhiteSpace(materia)) cmd.Parameters.AddWithValue("@Materia", materia.Trim());

                using var dr = await cmd.ExecuteReaderAsync();
                while (await dr.ReadAsync())
                {
                    estadisticas.Add(new EstadisticaDTO
                    {
                        Concepto = dr["Concepto"].ToString() ?? string.Empty,
                        Cantidad = Convert.ToInt32(dr["Cantidad"]),
                        Recursos = dr["Recursos"] != DBNull.Value ? dr["Recursos"].ToString() ?? string.Empty : string.Empty
                    });
                }

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