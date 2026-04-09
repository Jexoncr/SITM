using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminResenasController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index(string q = "", string estado = "", string tipo = "")
        {
            ViewBag.Q = q;
            ViewBag.Estado = estado;
            ViewBag.Tipo = tipo;

            var query = db.Resenas
                .Include(r => r.User)
                .Include(r => r.Comercio)
                .Include(r => r.Evento)
                .Include(r => r.Lugar)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(r =>
                    (r.User.Nombre ?? "").Contains(q) ||
                    (r.User.Apellido ?? "").Contains(q) ||
                    (r.User.Email ?? "").Contains(q) ||
                    (r.Comercio.Nombre ?? "").Contains(q) ||
                    (r.Evento.Nombre ?? "").Contains(q) ||
                    (r.Lugar.Nombre ?? "").Contains(q) ||
                    (r.Comentario ?? "").Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(r => r.Estado == estado);

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(r => r.Tipo == tipo);

            var model = await query
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Aprobar(int id)
        {
            var resena = await db.Resenas.FirstOrDefaultAsync(r => r.Id == id);
            if (resena == null)
                return HttpNotFound();

            resena.Estado = "Aprobada";
            resena.FechaModeracion = DateTime.Now;
            resena.ModeradoPorUserId = User.Identity.GetUserId();
            resena.MotivoModeracion = null;

            await db.SaveChangesAsync();

            TempData["Ok"] = "Reseña aprobada correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Ocultar(int id)
        {
            var resena = await db.Resenas.FirstOrDefaultAsync(r => r.Id == id);
            if (resena == null)
                return HttpNotFound();

            resena.Estado = "Oculta";
            resena.FechaModeracion = DateTime.Now;
            resena.ModeradoPorUserId = User.Identity.GetUserId();
            resena.MotivoModeracion = "Ocultada por moderación administrativa.";

            await db.SaveChangesAsync();

            TempData["Ok"] = "Reseña oculta correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Rechazar(int id, string motivo)
        {
            var resena = await db.Resenas.FirstOrDefaultAsync(r => r.Id == id);
            if (resena == null)
                return HttpNotFound();

            resena.Estado = "Rechazada";
            resena.FechaModeracion = DateTime.Now;
            resena.ModeradoPorUserId = User.Identity.GetUserId();
            resena.MotivoModeracion = string.IsNullOrWhiteSpace(motivo)
                ? "Rechazada por moderación administrativa."
                : motivo;

            await db.SaveChangesAsync();

            TempData["Ok"] = "Reseña rechazada correctamente.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}