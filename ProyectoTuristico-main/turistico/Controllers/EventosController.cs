using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    public class EventosController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 9;

        public async Task<ActionResult> Index(int pagina = 1)
        {
            var hoy = DateTime.Now;

            if (pagina < 1)
                pagina = 1;

            var eventosQuery = db.Eventos
                .Include(e => e.CategoriaEvento)
                .Include(e => e.Lugar)
                .Include(e => e.Comercio)
                .Include(e => e.Reservas)
                .Where(e =>
                    e.FechaInicio == null ||
                    (e.FechaFin.HasValue ? e.FechaFin >= hoy : e.FechaInicio >= hoy.Date))
                .OrderBy(e => e.FechaInicio);

            var totalRegistros = await eventosQuery.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize);

            if (totalPaginas == 0)
                totalPaginas = 1;

            if (pagina > totalPaginas)
                pagina = totalPaginas;

            var eventos = await eventosQuery
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var items = eventos.Select(e =>
            {
                var reservados = e.Reservas != null
                    ? e.Reservas.Where(r => r.Estado != "Cancelada").Sum(r => (int?)r.CantidadPersonas) ?? 0
                    : 0;

                var disponibles = e.CupoMaximo - reservados;
                if (disponibles < 0) disponibles = 0;

                return new EventoListaVM
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Categoria = e.CategoriaEvento != null ? e.CategoriaEvento.Nombre : "General",
                    LugarNombre = e.Lugar != null ? e.Lugar.Nombre : "Lugar no definido",
                    FechaInicio = e.FechaInicio,
                    CuposDisponibles = disponibles,
                    ImagenUrl = e.ImagenUrl
                };
            }).ToList();

            var model = new PaginacionVM<EventoListaVM>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            return View(model);
        }

        public async Task<ActionResult> Detalle(int id)
        {
            var evento = await db.Eventos
                .Include(e => e.CategoriaEvento)
                .Include(e => e.Lugar)
                .Include(e => e.Comercio)
                .Include(e => e.Reservas)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
                return HttpNotFound();

            var reservados = evento.Reservas
                .Where(r => r.Estado != "Cancelada")
                .Sum(r => (int?)r.CantidadPersonas) ?? 0;

            var disponibles = evento.CupoMaximo - reservados;
            if (disponibles < 0) disponibles = 0;

            var model = new EventoDetalleVM
            {
                Id = evento.Id,
                Nombre = evento.Nombre,
                Descripcion = evento.Descripcion,
                Categoria = evento.CategoriaEvento != null ? evento.CategoriaEvento.Nombre : "General",
                ComercioNombre = evento.Comercio != null ? evento.Comercio.Nombre : null,
                LugarNombre = evento.Lugar != null ? evento.Lugar.Nombre : null,
                Direccion = evento.Lugar != null ? evento.Lugar.Direccion : null,
                FechaInicio = evento.FechaInicio,
                FechaFin = evento.FechaFin,
                CuposDisponibles = disponibles,
                Telefono = evento.Comercio != null ? evento.Comercio.Telefono : evento.Lugar?.Telefono,
                WhatsApp = evento.Comercio != null ? evento.Comercio.LinkWhatsApp : null,
                SitioWeb = evento.Lugar != null ? evento.Lugar.SitioWeb : null,
                LimitePorPersona = evento.LimitePorPersona,
                ImagenUrl = evento.ImagenUrl
            };

            return View(model);
        }

        public async Task<ActionResult> Contacto(int id)
        {
            var evento = await db.Eventos
                .Include(e => e.Comercio)
                .Include(e => e.Lugar)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
                return HttpNotFound();

            return View(evento);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Reservar(EventoReservaVM vm)
        {
            var userId = User.Identity.GetUserId();

            if (!ModelState.IsValid || vm.NumeroPersonas <= 0)
            {
                TempData["Err"] = "Revisa la cantidad de personas.";
                return RedirectToAction("Detalle", new { id = vm.EventoId });
            }

            var evento = await db.Eventos
                .Include(e => e.Lugar)
                .Include(e => e.Reservas)
                .FirstOrDefaultAsync(e => e.Id == vm.EventoId);

            if (evento == null)
            {
                TempData["Err"] = "El evento no existe.";
                return RedirectToAction("Index");
            }

            var limitePorPersona = evento.LimitePorPersona > 0 ? evento.LimitePorPersona : 1;

            if (vm.NumeroPersonas > limitePorPersona)
            {
                TempData["Err"] = "La cantidad supera el límite permitido por persona para este evento.";
                return RedirectToAction("Detalle", new { id = vm.EventoId });
            }

            var yaReservadoPorUsuario = evento.Reservas
                .Where(r => r.UserId == userId && r.Estado != "Cancelada")
                .Sum(r => (int?)r.CantidadPersonas) ?? 0;

            if ((yaReservadoPorUsuario + vm.NumeroPersonas) > limitePorPersona)
            {
                TempData["Err"] = "Ya tienes una reserva previa para este evento. Con esta nueva solicitud superarías el límite permitido por persona.";
                return RedirectToAction("Detalle", new { id = vm.EventoId });
            }

            var reservados = evento.Reservas
                .Where(r => r.Estado != "Cancelada")
                .Sum(r => (int?)r.CantidadPersonas) ?? 0;

            var disponibles = evento.CupoMaximo - reservados;

            if (vm.NumeroPersonas > disponibles)
            {
                TempData["Err"] = "No hay suficientes cupos disponibles para esta reserva.";
                return RedirectToAction("Detalle", new { id = vm.EventoId });
            }

            var reserva = new Reserva
            {
                UserId = userId,
                LugarId = evento.LugarId,
                EventoId = evento.Id,
                FechaReserva = DateTime.Now,
                CantidadPersonas = vm.NumeroPersonas,
                Estado = "Confirmada"
            };

            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();

            TempData["Ok"] = "Reserva realizada correctamente.";
            return RedirectToAction("Index", "Reservas");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class EventoListaVM
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public string LugarNombre { get; set; }
        public DateTime? FechaInicio { get; set; }
        public int CuposDisponibles { get; set; }
        public string ImagenUrl { get; set; }
    }

    public class EventoDetalleVM
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public string ComercioNombre { get; set; }
        public string LugarNombre { get; set; }
        public string Direccion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int CuposDisponibles { get; set; }
        public int LimitePorPersona { get; set; }
        public string Telefono { get; set; }
        public string WhatsApp { get; set; }
        public string SitioWeb { get; set; }
        public string ImagenUrl { get; set; }
    }

    public class EventoReservaVM
    {
        public int EventoId { get; set; }
        public int NumeroPersonas { get; set; }
    }
}