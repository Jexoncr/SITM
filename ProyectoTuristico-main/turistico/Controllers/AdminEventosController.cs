using System;
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
    [Authorize(Roles = "Admin")]
    public class AdminEventosController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 8;

        public async Task<ActionResult> Index(string q = "", string categoria = "", int pagina = 1)
        {
            q = (q ?? "").Trim();
            categoria = (categoria ?? "").Trim();

            if (pagina < 1)
                pagina = 1;

            var query = db.Eventos
                .Include(e => e.Comercio)
                .Include(e => e.CategoriaEvento)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();

                query = query.Where(e =>
                    (e.Nombre ?? "").ToLower().Contains(qq) ||
                    (e.Descripcion ?? "").ToLower().Contains(qq) ||
                    (e.Comercio != null && (e.Comercio.Nombre ?? "").ToLower().Contains(qq)) ||
                    (e.CategoriaEvento != null && (e.CategoriaEvento.Nombre ?? "").ToLower().Contains(qq)));
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                var cc = categoria.ToLower();
                query = query.Where(e => e.CategoriaEvento != null && (e.CategoriaEvento.Nombre ?? "").ToLower().Contains(cc));
            }

            query = query.OrderByDescending(e => e.FechaInicio).ThenBy(e => e.Nombre);

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize);

            if (totalPaginas == 0)
                totalPaginas = 1;

            if (pagina > totalPaginas)
                pagina = totalPaginas;

            var items = await query
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Categoria = categoria;

            var vm = new PaginacionVM<Evento>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            return View(vm);
        }

        public async Task<ActionResult> Create()
        {
            await CargarCombosAsync();
            return View(new EventoAdminVM
            {
                CupoMaximo = 0,
                LimitePorPersona = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(EventoAdminVM vm)
        {
            await CargarCombosAsync(vm.ComercioId, vm.CategoriaEventoId);

            if (vm.LimitePorPersona <= 0)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona debe ser mayor que cero.");

            if (vm.CupoMaximo < 0)
                ModelState.AddModelError("CupoMaximo", "El cupo máximo no puede ser negativo.");

            if (vm.CupoMaximo > 0 && vm.LimitePorPersona > vm.CupoMaximo)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona no puede ser mayor que el cupo máximo.");

            if (vm.FechaInicio.HasValue && vm.FechaFin.HasValue && vm.FechaFin < vm.FechaInicio)
                ModelState.AddModelError("", "La fecha fin no puede ser menor que la fecha inicio.");

            if (!ModelState.IsValid)
                return View(vm);

            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .FirstOrDefaultAsync(c => c.Id == vm.ComercioId);

            if (comercio == null)
            {
                ModelState.AddModelError("ComercioId", "El comercio seleccionado no existe.");
                return View(vm);
            }

            var evento = new Evento
            {
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion,
                ComercioId = vm.ComercioId.Value,
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
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int id)
        {
            var evento = await db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
            if (evento == null)
                return HttpNotFound();

            await CargarCombosAsync(evento.ComercioId, evento.CategoriaEventoId);

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
        public async Task<ActionResult> Edit(EventoAdminVM vm)
        {
            await CargarCombosAsync(vm.ComercioId, vm.CategoriaEventoId);

            if (vm.LimitePorPersona <= 0)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona debe ser mayor que cero.");

            if (vm.CupoMaximo < 0)
                ModelState.AddModelError("CupoMaximo", "El cupo máximo no puede ser negativo.");

            if (vm.CupoMaximo > 0 && vm.LimitePorPersona > vm.CupoMaximo)
                ModelState.AddModelError("LimitePorPersona", "El límite por persona no puede ser mayor que el cupo máximo.");

            if (vm.FechaInicio.HasValue && vm.FechaFin.HasValue && vm.FechaFin < vm.FechaInicio)
                ModelState.AddModelError("", "La fecha fin no puede ser menor que la fecha inicio.");

            if (!ModelState.IsValid)
                return View(vm);

            var evento = await db.Eventos.FirstOrDefaultAsync(e => e.Id == vm.Id);
            if (evento == null)
                return HttpNotFound();

            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .FirstOrDefaultAsync(c => c.Id == vm.ComercioId);

            if (comercio == null)
            {
                ModelState.AddModelError("ComercioId", "El comercio seleccionado no existe.");
                return View(vm);
            }

            evento.Nombre = vm.Nombre;
            evento.Descripcion = vm.Descripcion;
            evento.ComercioId = vm.ComercioId.Value;
            evento.LugarId = comercio.LugarId;
            evento.CategoriaEventoId = vm.CategoriaEventoId;
            evento.FechaInicio = vm.FechaInicio;
            evento.FechaFin = vm.FechaFin;
            evento.CupoMaximo = vm.CupoMaximo;
            evento.LimitePorPersona = vm.LimitePorPersona;

            await db.SaveChangesAsync();
            await GuardarImagenEventoAsync(vm.ImagenArchivo, evento);

            TempData["Ok"] = "Evento actualizado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var evento = await db.Eventos
                .Include(e => e.Reservas)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                TempData["Err"] = "El evento no existe.";
                return RedirectToAction("Index");
            }

            var tieneReservas = evento.Reservas != null && evento.Reservas.Any(r => r.Estado != "Cancelada");
            if (tieneReservas)
            {
                TempData["Err"] = "No se puede eliminar el evento porque tiene reservas relacionadas.";
                return RedirectToAction("Index");
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

            return RedirectToAction("Index");
        }

        private async Task CargarCombosAsync(int? comercioId = null, int? categoriaEventoId = null)
        {
            ViewBag.Comercios = new SelectList(
                await db.Comercios.OrderBy(c => c.Nombre).ToListAsync(),
                "Id",
                "Nombre",
                comercioId
            );

            ViewBag.CategoriasEvento = new SelectList(
                await db.CategoriasEvento.OrderBy(c => c.Nombre).ToListAsync(),
                "Id",
                "Nombre",
                categoriaEventoId
            );
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}