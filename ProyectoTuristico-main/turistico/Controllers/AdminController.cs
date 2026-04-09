using ClosedXML.Excel;
using System;
using System.Data.Entity;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Dashboard()
        {
            var model = await ConstruirDashboardAsync();
            return View(model);
        }

        public async Task<FileResult> ExportarDashboardCsv()
        {
            var model = await ConstruirDashboardAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Indicador,Valor");
            sb.AppendLine($"Comercios registrados,{model.ComerciosRegistrados}");
            sb.AppendLine($"Comercios pendientes,{model.ComerciosPendientes}");
            sb.AppendLine($"Comercios aprobados,{model.ComerciosAprobados}");
            sb.AppendLine($"Comercios rechazados,{model.ComerciosRechazados}");
            sb.AppendLine($"Usuarios registrados,{model.UsuariosRegistrados}");
            sb.AppendLine($"Eventos activos,{model.EventosActivos}");
            sb.AppendLine($"Reseñas pendientes,{model.ResenasPendientes}");
            sb.AppendLine($"Reservas registradas,{model.ReservasRegistradas}");

            return File(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv",
                "dashboard-sitm.csv"
            );
        }

        public async Task<FileResult> ExportarDashboardExcel()
        {
            var model = await ConstruirDashboardAsync();

            using (var workbook = new XLWorkbook())
            {
                var resumen = workbook.Worksheets.Add("Resumen Ejecutivo");
                var indicadores = workbook.Worksheets.Add("Indicadores");

                ConstruirHojaResumen(resumen, model);
                ConstruirHojaIndicadores(indicadores, model);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"dashboard-sitm-{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                    );
                }
            }
        }

        public ActionResult ExportarDashboardPdf()
        {
            return RedirectToAction("DashboardPrint");
        }

        public async Task<ActionResult> DashboardPrint()
        {
            var model = await ConstruirDashboardAsync();
            return View("DashboardPrint", model);
        }

        private void ConstruirHojaResumen(IXLWorksheet ws, AdminDashboardVM model)
        {
            ws.Cell("A1").Value = "Dashboard Administrativo - SITM";
            ws.Range("A1:H1").Merge();
            ws.Range("A1:H1").Style.Font.Bold = true;
            ws.Range("A1:H1").Style.Font.FontSize = 22;
            ws.Range("A1:H1").Style.Font.FontColor = XLColor.White;
            ws.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.FromHtml("#145544");
            ws.Range("A1:H1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("A1:H1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 32;

            ws.Cell("A2").Value = "Resumen ejecutivo de indicadores administrativos";
            ws.Range("A2:H2").Merge();
            ws.Range("A2:H2").Style.Font.Italic = true;
            ws.Range("A2:H2").Style.Font.FontColor = XLColor.FromHtml("#475569");
            ws.Range("A2:H2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(2).Height = 22;

            ws.Cell("A3").Value = $"Fecha de exportación: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
            ws.Range("A3:H3").Merge();
            ws.Range("A3:H3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range("A3:H3").Style.Font.FontColor = XLColor.FromHtml("#334155");
            ws.Row(3).Height = 20;

            PintarKpi(ws, "A5:B7", "Comercios", model.ComerciosRegistrados, "#145544", "bi-shop");
            PintarKpi(ws, "C5:D7", "Usuarios", model.UsuariosRegistrados, "#1F8A6C", "bi-people");
            PintarKpi(ws, "E5:F7", "Eventos activos", model.EventosActivos, "#2563EB", "bi-calendar-event");
            PintarKpi(ws, "G5:H7", "Reservas", model.ReservasRegistradas, "#7C3AED", "bi-bookmark-check");

            ws.Cell("A9").Value = "Estado de comercios";
            ws.Range("A9:D9").Merge();
            ws.Range("A9:D9").Style.Font.Bold = true;
            ws.Range("A9:D9").Style.Font.FontSize = 14;
            ws.Range("A9:D9").Style.Font.FontColor = XLColor.White;
            ws.Range("A9:D9").Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            ws.Range("A9:D9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A10").Value = "Indicador";
            ws.Cell("B10").Value = "Valor";
            ws.Cell("C10").Value = "Porcentaje";
            ws.Cell("D10").Value = "Observación";

            var encabezado = ws.Range("A10:D10");
            encabezado.Style.Font.Bold = true;
            encabezado.Style.Font.FontColor = XLColor.White;
            encabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F8A6C");
            encabezado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            encabezado.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell("A11").Value = "Pendientes";
            ws.Cell("B11").Value = model.ComerciosPendientes;
            ws.Cell("C11").FormulaA1 = "IF($B$15=0,0,B11/$B$15)";
            ws.Cell("D11").Value = "Comercios en revisión";

            ws.Cell("A12").Value = "Aprobados";
            ws.Cell("B12").Value = model.ComerciosAprobados;
            ws.Cell("C12").FormulaA1 = "IF($B$15=0,0,B12/$B$15)";
            ws.Cell("D12").Value = "Comercios activos/aprobados";

            ws.Cell("A13").Value = "Rechazados";
            ws.Cell("B13").Value = model.ComerciosRechazados;
            ws.Cell("C13").FormulaA1 = "IF($B$15=0,0,B13/$B$15)";
            ws.Cell("D13").Value = "Comercios descartados";

            ws.Cell("A14").Value = "Total categorizado";
            ws.Cell("B14").FormulaA1 = "SUM(B11:B13)";
            ws.Cell("C14").Value = 1;
            ws.Cell("D14").Value = "Suma de estados";

            ws.Cell("A15").Value = "Comercios registrados";
            ws.Cell("B15").Value = model.ComerciosRegistrados;
            ws.Cell("C15").FormulaA1 = "IF(B15=0,0,B14/B15)";
            ws.Cell("D15").Value = "Base total registrada";

            var tablaResumen = ws.Range("A10:D15");
            tablaResumen.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tablaResumen.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tablaResumen.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Range("C11:C15").Style.NumberFormat.Format = "0.00%";

            for (int i = 11; i <= 15; i++)
            {
                if (i % 2 == 1)
                {
                    ws.Range($"A{i}:D{i}").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
            }

            ws.Cell("F9").Value = "Alertas y seguimiento";
            ws.Range("F9:H9").Merge();
            ws.Range("F9:H9").Style.Font.Bold = true;
            ws.Range("F9:H9").Style.Font.FontSize = 14;
            ws.Range("F9:H9").Style.Font.FontColor = XLColor.White;
            ws.Range("F9:H9").Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            ws.Range("F9:H9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("F10").Value = "Indicador";
            ws.Cell("G10").Value = "Valor";
            ws.Cell("H10").Value = "Estado";

            var encabezado2 = ws.Range("F10:H10");
            encabezado2.Style.Font.Bold = true;
            encabezado2.Style.Font.FontColor = XLColor.White;
            encabezado2.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            encabezado2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            encabezado2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell("F11").Value = "Reseñas pendientes";
            ws.Cell("G11").Value = model.ResenasPendientes;
            ws.Cell("H11").Value = model.ResenasPendientes > 0 ? "Atención requerida" : "Sin pendientes";

            ws.Cell("F12").Value = "Eventos activos";
            ws.Cell("G12").Value = model.EventosActivos;
            ws.Cell("H12").Value = model.EventosActivos > 0 ? "Operando" : "Sin actividad";

            ws.Cell("F13").Value = "Usuarios registrados";
            ws.Cell("G13").Value = model.UsuariosRegistrados;
            ws.Cell("H13").Value = model.UsuariosRegistrados > 0 ? "Base disponible" : "Sin usuarios";

            ws.Cell("F14").Value = "Reservas registradas";
            ws.Cell("G14").Value = model.ReservasRegistradas;
            ws.Cell("H14").Value = model.ReservasRegistradas > 0 ? "Con movimiento" : "Sin reservas";

            var tablaAlertas = ws.Range("F10:H14");
            tablaAlertas.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tablaAlertas.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tablaAlertas.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            for (int i = 11; i <= 14; i++)
            {
                if (i % 2 == 1)
                {
                    ws.Range($"F{i}:H{i}").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
            }

            ws.Columns("A:H").AdjustToContents();

            if (ws.Column("A").Width < 22) ws.Column("A").Width = 22;
            if (ws.Column("B").Width < 14) ws.Column("B").Width = 14;
            if (ws.Column("C").Width < 14) ws.Column("C").Width = 14;
            if (ws.Column("D").Width < 24) ws.Column("D").Width = 24;
            if (ws.Column("F").Width < 20) ws.Column("F").Width = 20;
            if (ws.Column("G").Width < 12) ws.Column("G").Width = 12;
            if (ws.Column("H").Width < 18) ws.Column("H").Width = 18;

            ws.SheetView.FreezeRows(10);
            ws.RangeUsed().Style.Alignment.WrapText = true;
        }

        private void ConstruirHojaIndicadores(IXLWorksheet ws, AdminDashboardVM model)
        {
            ws.Cell("A1").Value = "Indicadores Detallados";
            ws.Range("A1:E1").Merge();
            ws.Range("A1:E1").Style.Font.Bold = true;
            ws.Range("A1:E1").Style.Font.FontSize = 20;
            ws.Range("A1:E1").Style.Font.FontColor = XLColor.White;
            ws.Range("A1:E1").Style.Fill.BackgroundColor = XLColor.FromHtml("#145544");
            ws.Range("A1:E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(1).Height = 30;

            ws.Cell("A3").Value = "ID";
            ws.Cell("B3").Value = "Indicador";
            ws.Cell("C3").Value = "Valor";
            ws.Cell("D3").Value = "Clasificación";
            ws.Cell("E3").Value = "Detalle";

            var header = ws.Range("A3:E3");
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F8A6C");
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell("A4").Value = 1;
            ws.Cell("B4").Value = "Comercios registrados";
            ws.Cell("C4").Value = model.ComerciosRegistrados;
            ws.Cell("D4").Value = "General";
            ws.Cell("E4").Value = "Total de comercios existentes en el sistema";

            ws.Cell("A5").Value = 2;
            ws.Cell("B5").Value = "Comercios pendientes";
            ws.Cell("C5").Value = model.ComerciosPendientes;
            ws.Cell("D5").Value = "Estado";
            ws.Cell("E5").Value = "Comercios en proceso de revisión";

            ws.Cell("A6").Value = 3;
            ws.Cell("B6").Value = "Comercios aprobados";
            ws.Cell("C6").Value = model.ComerciosAprobados;
            ws.Cell("D6").Value = "Estado";
            ws.Cell("E6").Value = "Comercios aprobados y habilitados";

            ws.Cell("A7").Value = 4;
            ws.Cell("B7").Value = "Comercios rechazados";
            ws.Cell("C7").Value = model.ComerciosRechazados;
            ws.Cell("D7").Value = "Estado";
            ws.Cell("E7").Value = "Comercios rechazados por validación";

            ws.Cell("A8").Value = 5;
            ws.Cell("B8").Value = "Usuarios registrados";
            ws.Cell("C8").Value = model.UsuariosRegistrados;
            ws.Cell("D8").Value = "Usuarios";
            ws.Cell("E8").Value = "Cantidad total de usuarios del sistema";

            ws.Cell("A9").Value = 6;
            ws.Cell("B9").Value = "Eventos activos";
            ws.Cell("C9").Value = model.EventosActivos;
            ws.Cell("D9").Value = "Eventos";
            ws.Cell("E9").Value = "Eventos con fecha actual o futura";

            ws.Cell("A10").Value = 7;
            ws.Cell("B10").Value = "Reseñas pendientes";
            ws.Cell("C10").Value = model.ResenasPendientes;
            ws.Cell("D10").Value = "Moderación";
            ws.Cell("E10").Value = "Reseñas pendientes de aprobación";

            ws.Cell("A11").Value = 8;
            ws.Cell("B11").Value = "Reservas registradas";
            ws.Cell("C11").Value = model.ReservasRegistradas;
            ws.Cell("D11").Value = "Reservas";
            ws.Cell("E11").Value = "Total de reservas registradas";

            ws.Cell("A13").Value = "Métricas calculadas";
            ws.Range("A13:E13").Merge();
            ws.Range("A13:E13").Style.Font.Bold = true;
            ws.Range("A13:E13").Style.Font.FontSize = 14;
            ws.Range("A13:E13").Style.Font.FontColor = XLColor.White;
            ws.Range("A13:E13").Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            ws.Range("A13:E13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A14").Value = "Indicador";
            ws.Cell("B14").Value = "Resultado";
            ws.Cell("C14").Value = "Tipo";
            ws.Cell("D14").Value = "Lectura";
            ws.Cell("E14").Value = "Referencia";

            var header2 = ws.Range("A14:E14");
            header2.Style.Font.Bold = true;
            header2.Style.Font.FontColor = XLColor.White;
            header2.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            header2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell("A15").Value = "% aprobación";
            ws.Cell("B15").FormulaA1 = "IF(C5+C6+C7=0,0,C6/(C5+C6+C7))";
            ws.Cell("C15").Value = "Porcentaje";
            ws.Cell("D15").Value = "Participación de aprobados";
            ws.Cell("E15").Value = "Basado en estados";

            ws.Cell("A16").Value = "% pendientes";
            ws.Cell("B16").FormulaA1 = "IF(C5+C6+C7=0,0,C5/(C5+C6+C7))";
            ws.Cell("C16").Value = "Porcentaje";
            ws.Cell("D16").Value = "Participación de pendientes";
            ws.Cell("E16").Value = "Basado en estados";

            ws.Cell("A17").Value = "% rechazados";
            ws.Cell("B17").FormulaA1 = "IF(C5+C6+C7=0,0,C7/(C5+C6+C7))";
            ws.Cell("C17").Value = "Porcentaje";
            ws.Cell("D17").Value = "Participación de rechazados";
            ws.Cell("E17").Value = "Basado en estados";

            ws.Cell("A18").Value = "Ratio reseñas/comercios";
            ws.Cell("B18").FormulaA1 = "IF(C4=0,0,C10/C4)";
            ws.Cell("C18").Value = "Ratio";
            ws.Cell("D18").Value = "Carga de moderación";
            ws.Cell("E18").Value = "Reseñas pendientes sobre comercios";

            ws.Cell("A19").Value = "Ratio reservas/comercios";
            ws.Cell("B19").FormulaA1 = "IF(C4=0,0,C11/C4)";
            ws.Cell("C19").Value = "Ratio";
            ws.Cell("D19").Value = "Movimiento operativo";
            ws.Cell("E19").Value = "Reservas sobre comercios";

            ws.Cell("A20").Value = "Ratio eventos/comercios";
            ws.Cell("B20").FormulaA1 = "IF(C4=0,0,C9/C4)";
            ws.Cell("C20").Value = "Ratio";
            ws.Cell("D20").Value = "Actividad de eventos";
            ws.Cell("E20").Value = "Eventos activos sobre comercios";

            var tabla1 = ws.Range("A3:E11");
            tabla1.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tabla1.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tabla1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var tabla2 = ws.Range("A14:E20");
            tabla2.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tabla2.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tabla2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            for (int i = 4; i <= 11; i++)
            {
                if (i % 2 == 0)
                {
                    ws.Range($"A{i}:E{i}").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
            }

            for (int i = 15; i <= 20; i++)
            {
                if (i % 2 == 1)
                {
                    ws.Range($"A{i}:E{i}").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
            }

            ws.Range("B15:B17").Style.NumberFormat.Format = "0.00%";
            ws.Range("B18:B20").Style.NumberFormat.Format = "0.00";

            ws.Range("A3:E20").SetAutoFilter();
            ws.SheetView.FreezeRows(3);
            ws.Columns("A:E").AdjustToContents();

            if (ws.Column("A").Width < 14) ws.Column("A").Width = 14;
            if (ws.Column("B").Width < 28) ws.Column("B").Width = 28;
            if (ws.Column("C").Width < 14) ws.Column("C").Width = 14;
            if (ws.Column("D").Width < 18) ws.Column("D").Width = 18;
            if (ws.Column("E").Width < 30) ws.Column("E").Width = 30;

            ws.RangeUsed().Style.Alignment.WrapText = true;
        }

        private void PintarKpi(IXLWorksheet ws, string rangeAddress, string titulo, int valor, string colorHex, string icono)
        {
            var range = ws.Range(rangeAddress);
            range.Merge();
            range.Style.Fill.BackgroundColor = XLColor.FromHtml(colorHex);
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var firstCell = range.FirstCell();
            firstCell.Value = $"{titulo}\n{valor}";
            firstCell.Style.Font.Bold = true;
            firstCell.Style.Font.FontSize = 18;
            firstCell.Style.Alignment.WrapText = true;
        }

        private async Task<AdminDashboardVM> ConstruirDashboardAsync()
        {
            var hoy = DateTime.Today;

            return new AdminDashboardVM
            {
                ComerciosRegistrados = await db.Comercios.CountAsync(),
                ComerciosPendientes = await db.Comercios.CountAsync(x => x.Lugar != null && x.Lugar.Estado == "Pendiente"),
                ComerciosAprobados = await db.Comercios.CountAsync(x => x.Lugar != null && x.Lugar.Estado == "Aprobado"),
                ComerciosRechazados = await db.Comercios.CountAsync(x => x.Lugar != null && x.Lugar.Estado == "Rechazado"),
                UsuariosRegistrados = await db.Users.CountAsync(),
                EventosActivos = await db.Eventos.CountAsync(x => x.FechaInicio.HasValue && DbFunctions.TruncateTime(x.FechaInicio) >= hoy),
                ResenasPendientes = await db.Resenas.CountAsync(x => x.Estado == "Pendiente"),
                ReservasRegistradas = await db.Reservas.CountAsync()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}