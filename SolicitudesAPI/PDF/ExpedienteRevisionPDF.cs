using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SolicitudesShared.RecursosRevision;

namespace SolicitudesAPI
{
    public class ExpedienteRevisionPDF : IDocument
    {
        public ExpedienteRevisionDTO Datos { get; }

        // Colores institucionales
        private readonly string ColorPrimario = "#1E3A5F";      // Azul institucional oscuro
        private readonly string ColorFondoBloque = "#F8FAFC";   // Gris pizarra claro
        private readonly string ColorBorde = "#E2E8F0";         // Gris neutro
        private readonly string ColorTextoMuted = "#64748B";    // Texto secundario

        public ExpedienteRevisionPDF(ExpedienteRevisionDTO datos)
        {
            Datos = datos;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(35, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor(Colors.Grey.Darken4));

                page.Header().Element(ComponerEncabezado);
                page.Content().Element(ComponerCuerpo);
                page.Footer().Element(ComponerPiePagina);
            });
        }

        // -------------------------------------------------------------------------
        // 1. ENCABEZADO INSTITUCIONAL
        // -------------------------------------------------------------------------
        private void ComponerEncabezado(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("PODER JUDICIAL DEL ESTADO").Bold().FontSize(12).FontColor(ColorPrimario);
                        c.Item().Text("UNIDAD DE TRANSPARENCIA Y ACCESO A LA INFORMACIÓN").FontSize(8).FontColor(ColorTextoMuted);
                        c.Item().Text("FICHA TÉCNICA DE RECURSO DE REVISIÓN").Bold().FontSize(13).FontColor(ColorPrimario);
                    });

                    // Badge lateral con el número de recurso y estatus
                    row.ConstantItem(170).Border(1).BorderColor(ColorBorde).Background(ColorFondoBloque).Padding(8).Column(c =>
                    {
                        c.Item().Text("EXPEDIENTE").FontSize(7).Bold().FontColor(ColorTextoMuted);
                        c.Item().Text(Datos.NumeroRecurso ?? "SIN ASIGNAR").FontSize(11).Bold().FontColor(ColorPrimario);
                        c.Item().PaddingTop(2).Text($"Estatus: {Datos.Estatus ?? "En trámite"}").FontSize(8).FontColor(ColorPrimario);
                    });
                });

                col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColorPrimario);
            });
        }

        // -------------------------------------------------------------------------
        // 2. CUERPO DEL INFORME (4 BLOQUES)
        // -------------------------------------------------------------------------
        private void ComponerCuerpo(IContainer container)
        {
            container.PaddingTop(10).Column(col =>
            {
                col.Spacing(10);

                // BLOQUE A: DATOS GENERALES
                col.Item().Element(c => ComponerSeccion(c, "A. DATOS GENERALES DEL EXPEDIENTE", seccion =>
                {
                    seccion.Table(tabla =>
                    {
                        tabla.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Folio Solicitud", Datos.FolioSolicitud));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Materia", Datos.MateriaRecurso));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Fecha Presentación", FormatoFecha(Datos.FechaPresentacion)));

                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Recurrente", Datos.NombreRecurrente));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Responsable Interno", Datos.ResponsableInterno));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Documentos Digitalizados", $"{Datos.TotalDocumentos} archivo(s)"));
                    });
                }));

                // BLOQUE B: SOLICITUD ORIGINAL
                col.Item().Element(c => ComponerSeccion(c, "B. SOLICITUD ORIGINAL E INCONFORMIDAD", seccion =>
                {
                    seccion.Table(tabla =>
                    {
                        tabla.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Fecha Respuesta Original", FormatoFecha(Datos.FechaRespuestaSolicitud)));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Razón de Interposición", Datos.RazonInterposicion));

                        tabla.Cell().ColumnSpan(2).Element(EstiloCelda).Column(k => Campo(k, "Contenido de la Solicitud", Datos.ContenidoSolicitud));
                        tabla.Cell().ColumnSpan(2).Element(EstiloCelda).Column(k => Campo(k, "Respuesta Otorgada", Datos.RespuestaOtorgada));
                    });
                }));

                // BLOQUE C: TRÁMITE DEL RECURSO
                col.Item().Element(c => ComponerSeccion(c, "C. TRÁMITE PROCESAL Y RESOLUCIÓN", seccion =>
                {
                    seccion.Table(tabla =>
                    {
                        tabla.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Fecha Admisión", FormatoFecha(Datos.FechaAdmision)));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Fecha Notif. Admisión", FormatoFecha(Datos.FechaNotificacionAdmision)));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Límite Contestación", FormatoFecha(Datos.FechaLimiteContestacion)));

                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Fecha Contestación", FormatoFecha(Datos.FechaContestacionRecurso)));
                        tabla.Cell().ColumnSpan(2).Element(EstiloCelda).Column(k => Campo(k, "Sentido Contestación", Datos.SentidoContestacion));

                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Cierre Instrucción", FormatoFecha(Datos.FechaCierreInstruccion)));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Fecha Resolución", FormatoFecha(Datos.FechaResolucion)));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Sentido Resolución", Datos.SentidoResolucion));

                        tabla.Cell().ColumnSpan(3).Element(EstiloCelda).Column(k => Campo(k, "Fecha Notif. Resolución", FormatoFecha(Datos.FechaNotificacionResolucion)));
                    });
                }));

                // BLOQUE D: CUMPLIMIENTO Y OBSERVACIONES
                col.Item().Element(c => ComponerSeccion(c, "D. CUMPLIMIENTO Y OBSERVACIONES", seccion =>
                {
                    seccion.Table(tabla =>
                    {
                        tabla.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "¿Requiere Cumplimiento?", Datos.RequiereCumplimiento ? "Sí" : "No"));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "Fecha Límite Cumpl.", FormatoFecha(Datos.FechaLimiteCumplimiento)));
                        tabla.Cell().Element(EstiloCelda).Column(k => Campo(k, "¿Cumplió Resolución?", Datos.CumplioResolucion));

                        tabla.Cell().ColumnSpan(3).Element(EstiloCelda).Column(k => Campo(k, "Detalle Acuerdo Cumplimiento", Datos.AcuerdoCumplimiento));
                        tabla.Cell().ColumnSpan(3).Element(EstiloCelda).Column(k => Campo(k, "Observaciones Generales", Datos.Observaciones));
                    });
                }));
            });
        }

        // -------------------------------------------------------------------------
        // 3. PIE DE PÁGINA INSTITUCIONAL
        // -------------------------------------------------------------------------
        private void ComponerPiePagina(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(ColorBorde);
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Documento oficial generado el ").FontSize(7).FontColor(ColorTextoMuted);
                        t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold().FontSize(7).FontColor(ColorTextoMuted);
                    });

                    row.ConstantItem(120).AlignRight().Text(t =>
                    {
                        t.Span("Página ").FontSize(7).FontColor(ColorTextoMuted);
                        t.CurrentPageNumber().FontSize(7).FontColor(ColorTextoMuted);
                        t.Span(" de ").FontSize(7).FontColor(ColorTextoMuted);
                        t.TotalPages().FontSize(7).FontColor(ColorTextoMuted);
                    });
                });
            });
        }

        // -------------------------------------------------------------------------
        // MÉTODOS DE APOYO VISUAL
        // -------------------------------------------------------------------------
        private void ComponerSeccion(IContainer container, string titulo, Action<IContainer> contenido)
        {
            container.Border(1).BorderColor(ColorBorde).Column(col =>
            {
                // Barra de título del bloque
                col.Item()
                    .Background(ColorFondoBloque)
                    .PaddingVertical(4)
                    .PaddingHorizontal(8)
                    .Row(r =>
                    {
                        r.RelativeItem().Text(titulo).Bold().FontSize(8.5f).FontColor(ColorPrimario);
                    });

                col.Item().LineHorizontal(0.5f).LineColor(ColorBorde);

                // Contenido interno del bloque
                col.Item().Padding(4).Element(contenido);
            });
        }

        private static IContainer EstiloCelda(IContainer container)
        {
            return container.PaddingVertical(2).PaddingHorizontal(4);
        }

        private void Campo(ColumnDescriptor col, string etiqueta, string? valor)
        {
            col.Item().Text(etiqueta.ToUpper()).FontSize(6.5f).Bold().FontColor(ColorTextoMuted);
            col.Item().Text(string.IsNullOrWhiteSpace(valor) ? "—" : valor).FontSize(8).FontColor(Colors.Grey.Darken4);
        }

        private static string FormatoFecha(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy") ?? "—";
    }
}