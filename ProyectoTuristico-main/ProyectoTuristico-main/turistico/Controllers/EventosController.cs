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

        public async Task<ActionResult> Index()
        {

            var fechaMinima = DateTime.Today.AddDays(-1);

            var eventos = await db.Eventos
                .Include(e => e.CategoriaEvento)
                .Include(e => e.Lugar)
                .Include(e => e.Comercio)
                .Include(e => e.Reservas)
                .Where(e => !e.FechaInicio.HasValue || e.FechaInicio >= fechaMinima)
                .OrderBy(e => e.FechaInicio)
                .ToListAsync();

            var model = eventos.Select(e => new EventoListaVM
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Categoria = e.CategoriaEvento != null ? e.CategoriaEvento.Nombre : "Evento",
                Descripcion = e.Descripcion,
                LugarNombre = e.Comercio != null
                    ? e.Comercio.Nombre
                    : (e.Lugar != null ? e.Lugar.Nombre : "Ubicación"),
                FechaInicio = e.FechaInicio,
                FechaFin = e.FechaFin,
                CupoMaximo = e.CupoMaximo,
                CuposReservados = e.Reservas != null
                    ? e.Reservas
                        .Where(r => r.Estado != "Cancelada")
                        .Sum(r => (int?)r.NumeroPersonas) ?? 0
                    : 0
            }).ToList();

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

            var reservados = evento.Reservas != null
                ? evento.Reservas
                    .Where(r => r.Estado != "Cancelada")
                    .Sum(r => (int?)r.NumeroPersonas) ?? 0
                : 0;

            var model = new EventoDetalleVM
            {
                Id = evento.Id,
                Nombre = evento.Nombre,
                Descripcion = evento.Descripcion,
                Categoria = evento.CategoriaEvento != null ? evento.CategoriaEvento.Nombre : "Evento",
                ComercioNombre = evento.Comercio != null ? evento.Comercio.Nombre : "",
                LugarNombre = evento.Lugar != null ? evento.Lugar.Nombre : "",
                Direccion = evento.Lugar != null ? evento.Lugar.Direccion : "",
                Telefono = evento.Comercio != null
                    ? evento.Comercio.Telefono
                    : (evento.Lugar != null ? evento.Lugar.Telefono : ""),
                WhatsApp = evento.Comercio != null ? evento.Comercio.LinkWhatsApp : "",
                SitioWeb = evento.Lugar != null ? evento.Lugar.SitioWeb : "",
                FechaInicio = evento.FechaInicio,
                FechaFin = evento.FechaFin,
                CupoMaximo = evento.CupoMaximo,
                CuposReservados = reservados
            };

            return View(model);
        }

        public async Task<ActionResult> Contacto(int id)
        {
            var evento = await db.Eventos
                .Include(e => e.Lugar)
                .Include(e => e.Comercio)
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

            if (vm == null)
            {
                TempData["Err"] = "No se recibieron datos de la reserva.";
                return RedirectToAction("Index");
            }

            if (vm.NumeroPersonas <= 0)
            {
                TempData["Err"] = "La cantidad de personas debe ser mayor a cero.";
                return RedirectToAction("Detalle", new { id = vm.EventoId });
            }

            var evento = await db.Eventos
                .Include(e => e.Reservas)
                .FirstOrDefaultAsync(e => e.Id == vm.EventoId);

            if (evento == null)
            {
                TempData["Err"] = "El evento no existe.";
                return RedirectToAction("Index");
            }

            var reservados = evento.Reservas != null
                ? evento.Reservas
                    .Where(r => r.Estado != "Cancelada")
                    .Sum(r => (int?)r.NumeroPersonas) ?? 0
                : 0;

            var disponibles = evento.CupoMaximo > 0
                ? evento.CupoMaximo - reservados
                : int.MaxValue;

            if (evento.CupoMaximo > 0 && vm.NumeroPersonas > disponibles)
            {
                TempData["Err"] = "No hay suficientes cupos disponibles para esa cantidad de personas.";
                return RedirectToAction("Detalle", new { id = vm.EventoId });
            }

            var reserva = new Reserva
            {
                UserId = userId,
                LugarId = evento.LugarId,
                EventoId = evento.Id,
                FechaReserva = DateTime.Now,
                NumeroPersonas = vm.NumeroPersonas,
                Estado = "Confirmada"
            };

            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();

            TempData["Ok"] = "Reserva del evento creada correctamente.";
            return RedirectToAction("Index", "Reservas");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }

    public class EventoListaVM
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public string Descripcion { get; set; }
        public string LugarNombre { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int CupoMaximo { get; set; }
        public int CuposReservados { get; set; }

        public int CuposDisponibles
        {
            get
            {
                if (CupoMaximo <= 0) return 0;
                var disponibles = CupoMaximo - CuposReservados;
                return disponibles < 0 ? 0 : disponibles;
            }
        }
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
        public string Telefono { get; set; }
        public string WhatsApp { get; set; }
        public string SitioWeb { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int CupoMaximo { get; set; }
        public int CuposReservados { get; set; }

        public int CuposDisponibles
        {
            get
            {
                if (CupoMaximo <= 0) return 0;
                var disponibles = CupoMaximo - CuposReservados;
                return disponibles < 0 ? 0 : disponibles;
            }
        }
    }

    public class EventoReservaVM
    {
        public int EventoId { get; set; }
        public int NumeroPersonas { get; set; }
    }
}