using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Comercio")]
    public class ComercioPanelController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 6;

        private string UserIdActual => User.Identity.GetUserId();

        private async Task<Comercio> ObtenerComercioActualAsync()
        {
            return await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.Lugar.Categoria)
                .Include(c => c.ComercioRegulado)
                .Include(c => c.Eventos)
                .FirstOrDefaultAsync(c => c.UserId == UserIdActual);
        }

        private ActionResult RedirigirSinComercioAsignado()
        {
            TempData["Err"] = "Tu usuario de comercio todavía no tiene un comercio asignado.";
            return RedirectToAction("Index", "Home");
        }

        private async Task CargarCombosEventoAsync(int? categoriaEventoId = null)
        {
            var comercio = await ObtenerComercioActualAsync();

            ViewBag.CategoriasEvento = new SelectList(
                await db.CategoriasEvento.OrderBy(x => x.Nombre).ToListAsync(),
                "Id",
                "Nombre",
                categoriaEventoId
            );

            ViewBag.ComercioActual = comercio;
        }

        private async Task GuardarImagenEventoAsync(HttpPostedFileBase archivo, Evento evento)
        {
            if (archivo == null || archivo.ContentLength <= 0 || evento == null)
                return;

            var extension = Path.GetExtension(archivo.FileName)?.ToLowerInvariant();
            var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!permitidas.Contains(extension))
                return;

            if (archivo.ContentLength > 5 * 1024 * 1024)
                return;

            var carpeta = Server.MapPath("~/Content/img/eventos");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var nombre = Guid.NewGuid().ToString("N") + extension;
            var ruta = Path.Combine(carpeta, nombre);
            archivo.SaveAs(ruta);

            evento.ImagenUrl = "/Content/img/eventos/" + nombre;
            await db.SaveChangesAsync();
        }

        public async Task<ActionResult> Dashboard()
        {
            var comercio = await ObtenerComercioActualAsync();
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            var ahora = DateTime.Now;
            var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            var hace30Dias = ahora.AddDays(-30);

            var eventos = await db.Eventos
                .Include(e => e.CategoriaEvento)
                .Where(e => e.ComercioId == comercio.Id)
                .OrderByDescending(e => e.FechaInicio)
                .ToListAsync();

            var eventoIds = eventos.Select(e => e.Id).ToList();

            var resenas = await db.Resenas
                .Where(r => r.ComercioId == comercio.Id)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            var reservas = await db.Reservas
                .Include(r => r.Evento)
                .Include(r => r.User)
                .Where(r => r.EventoId.HasValue && eventoIds.Contains(r.EventoId.Value))
                .OrderByDescending(r => r.FechaReserva)
                .ToListAsync();

            var ultimosEventos = eventos
                .Take(5)
                .Select(e => new ComercioDashboardActividadVM
                {
                    Titulo = e.Nombre,
                    Subtitulo = e.CategoriaEvento != null ? e.CategoriaEvento.Nombre : "Evento",
                    Fecha = e.FechaInicio,
                    Tipo = "Evento"
                })
                .ToList();

            var ultimasResenas = resenas
                .Take(5)
                .Select(r => new ComercioDashboardActividadVM
                {
                    Titulo = "Nueva reseña",
                    Subtitulo = string.IsNullOrWhiteSpace(r.Comentario) ? "Sin comentario" : r.Comentario,
                    Fecha = r.Fecha,
                    Tipo = "Reseña"
                })
                .ToList();

            var ultimasReservas = reservas
                .Take(5)
                .Select(r => new ComercioDashboardActividadVM
                {
                    Titulo = r.Evento != null ? ("Reserva en " + r.Evento.Nombre) : "Nueva reserva",
                    Subtitulo = r.User != null
                        ? (string.IsNullOrWhiteSpace((r.User.Nombre + " " + r.User.Apellido).Trim())
                            ? r.User.Email
                            : (r.User.Nombre + " " + r.User.Apellido).Trim())
                        : "Usuario",
                    Fecha = r.FechaReserva,
                    Tipo = "Reserva"
                })
                .ToList();

            var actividadReciente = ultimosEventos
                .Concat(ultimasResenas)
                .Concat(ultimasReservas)
                .OrderByDescending(x => x.Fecha ?? DateTime.MinValue)
                .Take(8)
                .ToList();

            var eventosDelMes = eventos.Count(e => e.FechaInicio.HasValue && e.FechaInicio.Value >= inicioMes);
            var reservasDelMes = reservas.Count(r => r.FechaReserva >= inicioMes);
            var resenasPendientes = resenas.Count(r => r.Estado == "Pendiente");
            var resenasAprobadas = resenas.Count(r => r.Estado == "Aprobada");
            var resenasRechazadas = resenas.Count(r => r.Estado == "Rechazada");
            var promedioCalificacion = resenas.Any() ? resenas.Average(r => r.Calificacion) : 0;
            var eventosActivos = eventos.Count(e => !e.FechaFin.HasValue || e.FechaFin.Value >= ahora);
            var proximosEventos = eventos.Count(e => e.FechaInicio.HasValue && e.FechaInicio.Value >= ahora);
            var reservasUltimos30 = reservas.Count(r => r.FechaReserva >= hace30Dias);

            var chartEventosMeses = new List<string>();
            var chartEventosValores = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var mes = new DateTime(ahora.Year, ahora.Month, 1).AddMonths(-i);
                var sigMes = mes.AddMonths(1);

                chartEventosMeses.Add(mes.ToString("MMM"));
                chartEventosValores.Add(
                    eventos.Count(e => e.FechaInicio.HasValue &&
                                      e.FechaInicio.Value >= mes &&
                                      e.FechaInicio.Value < sigMes)
                );
            }

            var vm = new ComercioPanelDashboardVM
            {
                ComercioId = comercio.Id,
                NombreComercio = comercio.Nombre,
                Categoria = comercio.Lugar?.Categoria?.Nombre,
                Estado = comercio.Lugar?.Estado,
                TotalEventos = eventos.Count,
                EventosActivos = eventosActivos,
                TotalResenas = resenas.Count,
                ResenasPendientes = resenasPendientes,
                PromedioCalificacion = promedioCalificacion,
                TotalReservas = reservas.Count,
                EventosDelMes = eventosDelMes,
                ReservasDelMes = reservasDelMes,
                ProximosEventos = proximosEventos,
                ReservasUltimos30Dias = reservasUltimos30,
                ResenasAprobadas = resenasAprobadas,
                ResenasRechazadas = resenasRechazadas,
                ChartEventosMeses = chartEventosMeses,
                ChartEventosValores = chartEventosValores,
                ChartResenasLabels = new List<string> { "Pendientes", "Aprobadas", "Rechazadas" },
                ChartResenasValores = new List<int> { resenasPendientes, resenasAprobadas, resenasRechazadas },
                ActividadReciente = actividadReciente
            };

            return View(vm);
        }

        public async Task<ActionResult> MiComercio()
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.Lugar.Categoria)
                .Include(c => c.ComercioRegulado)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == UserIdActual);

            if (comercio == null)
                return RedirigirSinComercioAsignado();

            return View(comercio);
        }

        public async Task<ActionResult> MisEventos(int pagina = 1)
        {
            var comercio = await db.Comercios.FirstOrDefaultAsync(c => c.UserId == UserIdActual);
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            var query = db.Eventos
                .Include(e => e.CategoriaEvento)
                .Include(e => e.Lugar)
                .Where(e => e.ComercioId == comercio.Id)
                .OrderByDescending(e => e.FechaInicio);

            var totalRegistros = await query.CountAsync();

            var items = await query
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new PaginacionVM<Evento>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize),
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            return View(vm);
        }

        public async Task<ActionResult> CrearEvento()
        {
            var comercio = await ObtenerComercioActualAsync();
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            await CargarCombosEventoAsync();

            return View(new EventoAdminVM
            {
                ComercioId = comercio.Id,
                LugarId = comercio.LugarId,
                CupoMaximo = 0,
                LimitePorPersona = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearEvento(EventoAdminVM vm)
        {
            var comercio = await ObtenerComercioActualAsync();
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            vm.ComercioId = comercio.Id;
            vm.LugarId = comercio.LugarId;

            if (vm.LimitePorPersona <= 0)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona debe ser mayor que cero.");

            if (vm.CupoMaximo < 0)
                ModelState.AddModelError("CupoMaximo", "El cupo máximo no puede ser negativo.");

            if (vm.CupoMaximo > 0 && vm.LimitePorPersona > vm.CupoMaximo)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona no puede ser mayor que el cupo máximo.");

            if (vm.FechaInicio.HasValue && vm.FechaFin.HasValue && vm.FechaFin < vm.FechaInicio)
                ModelState.AddModelError("", "La fecha fin no puede ser menor que la fecha inicio.");

            if (!ModelState.IsValid)
            {
                await CargarCombosEventoAsync(vm.CategoriaEventoId);
                return View(vm);
            }

            var evento = new Evento
            {
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion,
                ComercioId = comercio.Id,
                LugarId = comercio.LugarId,
                CategoriaEventoId = vm.CategoriaEventoId,
                FechaInicio = vm.FechaInicio,
                FechaFin = vm.FechaFin,
                CupoMaximo = vm.CupoMaximo,
                LimitePorPersona = vm.LimitePorPersona
            };

            db.Eventos.Add(evento);
            await db.SaveChangesAsync();

            await GuardarImagenEventoAsync(vm.ImagenArchivo, evento);

            TempData["Ok"] = "Evento creado correctamente.";
            return RedirectToAction("MisEventos");
        }

        public async Task<ActionResult> EditarEvento(int id)
        {
            var comercio = await ObtenerComercioActualAsync();
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            var evento = await db.Eventos
                .FirstOrDefaultAsync(e => e.Id == id && e.ComercioId == comercio.Id);

            if (evento == null)
                return HttpNotFound();

            await CargarCombosEventoAsync(evento.CategoriaEventoId);
            ViewBag.ImagenActual = evento.ImagenUrl;

            var vm = new EventoAdminVM
            {
                Id = evento.Id,
                Nombre = evento.Nombre,
                Descripcion = evento.Descripcion,
                ComercioId = evento.ComercioId,
                LugarId = evento.LugarId,
                CategoriaEventoId = evento.CategoriaEventoId,
                FechaInicio = evento.FechaInicio,
                FechaFin = evento.FechaFin,
                CupoMaximo = evento.CupoMaximo,
                LimitePorPersona = evento.LimitePorPersona,
                ImagenUrlActual = evento.ImagenUrl
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarEvento(EventoAdminVM vm)
        {
            var comercio = await ObtenerComercioActualAsync();
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            var evento = await db.Eventos
                .FirstOrDefaultAsync(e => e.Id == vm.Id && e.ComercioId == comercio.Id);

            if (evento == null)
                return HttpNotFound();

            if (vm.LimitePorPersona <= 0)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona debe ser mayor que cero.");

            if (vm.CupoMaximo < 0)
                ModelState.AddModelError("CupoMaximo", "El cupo máximo no puede ser negativo.");

            if (vm.CupoMaximo > 0 && vm.LimitePorPersona > vm.CupoMaximo)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona no puede ser mayor que el cupo máximo.");

            if (vm.FechaInicio.HasValue && vm.FechaFin.HasValue && vm.FechaFin < vm.FechaInicio)
                ModelState.AddModelError("", "La fecha fin no puede ser menor que la fecha inicio.");

            if (!ModelState.IsValid)
            {
                await CargarCombosEventoAsync(vm.CategoriaEventoId);
                return View(vm);
            }

            evento.Nombre = vm.Nombre;
            evento.Descripcion = vm.Descripcion;
            evento.CategoriaEventoId = vm.CategoriaEventoId;
            evento.FechaInicio = vm.FechaInicio;
            evento.FechaFin = vm.FechaFin;
            evento.CupoMaximo = vm.CupoMaximo;
            evento.LimitePorPersona = vm.LimitePorPersona;

            await db.SaveChangesAsync();
            await GuardarImagenEventoAsync(vm.ImagenArchivo, evento);

            TempData["Ok"] = "Evento actualizado correctamente.";
            return RedirectToAction("MisEventos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarEvento(int id)
        {
            var comercio = await ObtenerComercioActualAsync();
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            var evento = await db.Eventos
                .Include(e => e.Reservas)
                .FirstOrDefaultAsync(e => e.Id == id && e.ComercioId == comercio.Id);

            if (evento == null)
                return HttpNotFound();

            var tieneReservas = evento.Reservas != null && evento.Reservas.Any(r => r.Estado != "Cancelada");
            if (tieneReservas)
            {
                TempData["Err"] = "No se puede eliminar el evento porque tiene reservas relacionadas.";
                return RedirectToAction("MisEventos");
            }

            try
            {
                db.Eventos.Remove(evento);
                await db.SaveChangesAsync();
                TempData["Ok"] = "Evento eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Err"] = "No se pudo eliminar el evento porque tiene información relacionada.";
            }
            catch (Exception)
            {
                TempData["Err"] = "Ocurrió un problema al eliminar el evento.";
            }

            return RedirectToAction("MisEventos");
        }

        public async Task<ActionResult> ReservasEventos(int pagina = 1)
        {
            var comercio = await ObtenerComercioActualAsync();
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            var eventoIds = await db.Eventos
                .Where(e => e.ComercioId == comercio.Id)
                .Select(e => e.Id)
                .ToListAsync();

            var query = db.Reservas
                .Include(r => r.User)
                .Include(r => r.Evento)
                .Where(r => r.EventoId.HasValue && eventoIds.Contains(r.EventoId.Value))
                .OrderByDescending(r => r.FechaReserva);

            var totalRegistros = await query.CountAsync();

            var items = await query
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new PaginacionVM<Reserva>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize),
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            return View(vm);
        }

        public async Task<ActionResult> MisResenas(int pagina = 1)
        {
            var comercio = await db.Comercios.FirstOrDefaultAsync(c => c.UserId == UserIdActual);
            if (comercio == null)
                return RedirigirSinComercioAsignado();

            var query = db.Resenas
                .Include(r => r.User)
                .Include(r => r.Evento)
                .Include(r => r.Comercio)
                .Where(r => r.ComercioId == comercio.Id)
                .OrderByDescending(r => r.Fecha);

            var totalRegistros = await query.CountAsync();

            var items = await query
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new PaginacionVM<Resena>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize),
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class ComercioPanelDashboardVM
    {
        public int ComercioId { get; set; }
        public string NombreComercio { get; set; }
        public string Categoria { get; set; }
        public string Estado { get; set; }

        public int TotalEventos { get; set; }
        public int EventosActivos { get; set; }
        public int TotalResenas { get; set; }
        public int ResenasPendientes { get; set; }
        public int TotalReservas { get; set; }
        public double PromedioCalificacion { get; set; }

        public int EventosDelMes { get; set; }
        public int ReservasDelMes { get; set; }
        public int ProximosEventos { get; set; }
        public int ReservasUltimos30Dias { get; set; }
        public int ResenasAprobadas { get; set; }
        public int ResenasRechazadas { get; set; }

        public List<string> ChartEventosMeses { get; set; }
        public List<int> ChartEventosValores { get; set; }
        public List<string> ChartResenasLabels { get; set; }
        public List<int> ChartResenasValores { get; set; }

        public List<ComercioDashboardActividadVM> ActividadReciente { get; set; }
    }

    public class ComercioDashboardActividadVM
    {
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public string Subtitulo { get; set; }
        public DateTime? Fecha { get; set; }
    }
}