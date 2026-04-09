using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();

            var reservas = await db.Reservas
                .Include(r => r.Evento)
                .Include(r => r.Lugar)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.FechaReserva)
                .ToListAsync();

            var model = reservas.Select(r => new ReservaUsuarioVM
            {
                Id = r.Id,
                Titulo = r.Evento != null ? r.Evento.Nombre : (r.Lugar != null ? r.Lugar.Nombre : "Reserva"),
                Tipo = r.Evento != null ? "Evento" : "Lugar",
                FechaInicio = r.Evento != null && r.Evento.FechaInicio.HasValue ? r.Evento.FechaInicio.Value : r.FechaReserva,
                FechaFin = r.Evento != null ? r.Evento.FechaFin : null,
                Personas = r.CantidadPersonas,
                Plan = r.Evento != null ? "Reserva de evento" : "Reserva",
                Estado = r.Estado,
                PuedeCancelar = r.Estado == "Pendiente" || r.Estado == "Confirmada"
            }).ToList();

            return View(model);
        }

        public async Task<ActionResult> DetalleReserva(int id)
        {
            var userId = User.Identity.GetUserId();

            var reserva = await db.Reservas
                .Include(r => r.Evento)
                .Include(r => r.Lugar)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reserva == null)
                return HttpNotFound();

            var model = new ReservaDetalleVM
            {
                Id = reserva.Id,
                Titulo = reserva.Evento != null ? reserva.Evento.Nombre : (reserva.Lugar != null ? reserva.Lugar.Nombre : "Reserva"),
                Tipo = reserva.Evento != null ? "Evento" : "Lugar",
                Estado = reserva.Estado,
                FechaReserva = reserva.FechaReserva,
                FechaInicio = reserva.Evento != null ? reserva.Evento.FechaInicio : null,
                FechaFin = reserva.Evento != null ? reserva.Evento.FechaFin : null,
                NumeroPersonas = reserva.CantidadPersonas,
                LugarNombre = reserva.Lugar != null ? reserva.Lugar.Nombre : null,
                Direccion = reserva.Lugar != null ? reserva.Lugar.Direccion : null
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelarReserva(int id)
        {
            var userId = User.Identity.GetUserId();

            var reserva = await db.Reservas
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reserva == null)
                return HttpNotFound();

            if (reserva.Estado != "Pendiente" && reserva.Estado != "Confirmada")
            {
                TempData["Err"] = "Esta reserva ya no puede cancelarse.";
                return RedirectToAction("Index");
            }

            reserva.Estado = "Cancelada";
            await db.SaveChangesAsync();

            TempData["Ok"] = "Reserva cancelada correctamente.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class ReservaUsuarioVM
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Tipo { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int Personas { get; set; }
        public string Plan { get; set; }
        public string Estado { get; set; }
        public bool PuedeCancelar { get; set; }
    }

    public class ReservaDetalleVM
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Tipo { get; set; }
        public string Estado { get; set; }
        public DateTime FechaReserva { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int NumeroPersonas { get; set; }
        public string LugarNombre { get; set; }
        public string Direccion { get; set; }
    }
}