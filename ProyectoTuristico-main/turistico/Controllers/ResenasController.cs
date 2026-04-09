using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    public class ResenasController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 8;

        public async Task<ActionResult> Index(int? comercioId, int? eventoId, int? lugarId, int pagina = 1)
        {
            var currentUserId = User.Identity.IsAuthenticated
                ? User.Identity.GetUserId()
                : null;

            if (pagina < 1)
                pagina = 1;

            var query = db.Resenas
                .Include(r => r.User)
                .Include(r => r.Comercio)
                .Include(r => r.Evento)
                .Include(r => r.Lugar)
                .Include(r => r.Imagenes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(r => r.Estado == "Aprobada" || r.UserId == currentUserId);
            }
            else
            {
                query = query.Where(r => r.Estado == "Aprobada");
            }

            if (comercioId.HasValue)
                query = query.Where(r => r.ComercioId == comercioId.Value);

            if (eventoId.HasValue)
                query = query.Where(r => r.EventoId == eventoId.Value);

            if (lugarId.HasValue)
                query = query.Where(r => r.LugarId == lugarId.Value);

            query = query.OrderByDescending(r => r.Fecha);

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize);

            if (totalPaginas == 0)
                totalPaginas = 1;

            if (pagina > totalPaginas)
                pagina = totalPaginas;

            var data = await query
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.ComercioId = comercioId;
            ViewBag.EventoId = eventoId;
            ViewBag.LugarId = lugarId;
            ViewBag.PaginaActual = pagina;
            ViewBag.ElementoNombre = await ObtenerNombreElementoAsync(comercioId, eventoId, lugarId);

            ViewBag.Comercios = await db.Comercios
                .OrderBy(c => c.Nombre)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Nombre
                })
                .ToListAsync();

            var model = new PaginacionVM<Resena>
            {
                Items = data,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            return View(model);
        }

        [Authorize]
        public async Task<ActionResult> Create(int? comercioId, int? eventoId, int? lugarId)
        {
            var vm = new ResenaCreateVM();

            if (comercioId.HasValue)
            {
                var comercio = await db.Comercios
                    .Include(c => c.Lugar)
                    .FirstOrDefaultAsync(c => c.Id == comercioId.Value);

                if (comercio == null)
                    return HttpNotFound();

                vm.ComercioId = comercio.Id;
                vm.LugarId = comercio.LugarId;
                vm.Tipo = "Comercio";
                ViewBag.ElementoNombre = comercio.Nombre;
            }
            else if (eventoId.HasValue)
            {
                var evento = await db.Eventos
                    .Include(e => e.Lugar)
                    .FirstOrDefaultAsync(e => e.Id == eventoId.Value);

                if (evento == null)
                    return HttpNotFound();

                vm.EventoId = evento.Id;
                vm.LugarId = evento.LugarId;
                vm.ComercioId = evento.ComercioId;
                vm.Tipo = "Evento";
                ViewBag.ElementoNombre = evento.Nombre;
            }
            else if (lugarId.HasValue)
            {
                var lugar = await db.Lugares.FirstOrDefaultAsync(l => l.Id == lugarId.Value);
                if (lugar == null)
                    return HttpNotFound();

                vm.LugarId = lugar.Id;
                vm.Tipo = "Lugar";
                ViewBag.ElementoNombre = lugar.Nombre;
            }
            else
            {
                TempData["Err"] = "No se indicó el elemento a reseñar.";
                return RedirectToAction("Index");
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ResenaCreateVM vm)
        {
            await NormalizarResenaAsync(vm);

            ModelState.Remove("LugarId");
            ModelState.Remove("Tipo");

            if (vm.LugarId.HasValue && vm.LugarId.Value > 0)
                ModelState.SetModelValue("LugarId", new ValueProviderResult(vm.LugarId, vm.LugarId.ToString(), null));

            if (!string.IsNullOrWhiteSpace(vm.Tipo))
                ModelState.SetModelValue("Tipo", new ValueProviderResult(vm.Tipo, vm.Tipo, null));

            if (!ModelState.IsValid)
            {
                TempData["Err"] = ObtenerErroresModelo();
                return RedirectToAction("Index", ConstruirRouteValues(vm));
            }

            if (vm.Tipo != "Comercio" && vm.Tipo != "Evento" && vm.Tipo != "Lugar")
            {
                TempData["Err"] = "El tipo de reseña no es válido.";
                return RedirectToAction("Index", ConstruirRouteValues(vm));
            }

            if (!vm.LugarId.HasValue || vm.LugarId.Value <= 0)
            {
                TempData["Err"] = "No se encontró el lugar asociado a la reseña.";
                return RedirectToAction("Index", ConstruirRouteValues(vm));
            }

            var resena = new Resena
            {
                UserId = User.Identity.GetUserId(),
                LugarId = vm.LugarId.Value,
                ComercioId = vm.ComercioId,
                EventoId = vm.EventoId,
                Tipo = vm.Tipo,
                Calificacion = vm.Calificacion,
                Comentario = vm.Comentario,
                Estado = "Pendiente",
                Fecha = DateTime.Now
            };

            db.Resenas.Add(resena);
            await db.SaveChangesAsync();

            if (vm.Imagenes != null && vm.Imagenes.Any())
            {
                var carpeta = Server.MapPath("~/Content/img/resenas");
                Directory.CreateDirectory(carpeta);

                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                foreach (var archivo in vm.Imagenes)
                {
                    if (archivo == null || archivo.ContentLength <= 0)
                        continue;

                    var extension = Path.GetExtension(archivo.FileName);
                    extension = string.IsNullOrWhiteSpace(extension) ? "" : extension.ToLower();

                    if (!extensionesPermitidas.Contains(extension))
                        continue;

                    if (archivo.ContentLength > 5 * 1024 * 1024)
                        continue;

                    var nombreArchivo = Guid.NewGuid().ToString("N") + extension;
                    var rutaFisica = Path.Combine(carpeta, nombreArchivo);
                    archivo.SaveAs(rutaFisica);

                    db.ResenaImagenes.Add(new ResenaImagen
                    {
                        ResenaId = resena.Id,
                        UrlImagen = "/Content/img/resenas/" + nombreArchivo
                    });
                }

                await db.SaveChangesAsync();
            }

            TempData["Ok"] = "Tu reseña fue enviada correctamente y quedó pendiente de moderación.";
            return RedirectToAction("Index", ConstruirRouteValues(vm));
        }

        private async Task NormalizarResenaAsync(ResenaCreateVM vm)
        {
            if (vm == null)
                return;

            vm.Tipo = (vm.Tipo ?? "").Trim();

            if (vm.ComercioId.HasValue && vm.ComercioId.Value > 0)
            {
                var comercio = await db.Comercios
                    .Include(c => c.Lugar)
                    .FirstOrDefaultAsync(c => c.Id == vm.ComercioId.Value);

                if (comercio == null)
                {
                    ModelState.AddModelError("ComercioId", "El comercio seleccionado no existe.");
                    return;
                }

                vm.Tipo = "Comercio";
                vm.LugarId = comercio.LugarId;
                vm.EventoId = null;
                return;
            }

            if (vm.EventoId.HasValue && vm.EventoId.Value > 0)
            {
                var evento = await db.Eventos
                    .Include(e => e.Lugar)
                    .FirstOrDefaultAsync(e => e.Id == vm.EventoId.Value);

                if (evento == null)
                {
                    ModelState.AddModelError("EventoId", "El evento seleccionado no existe.");
                    return;
                }

                vm.Tipo = "Evento";
                vm.LugarId = evento.LugarId;
                vm.ComercioId = evento.ComercioId;
                return;
            }

            if (vm.LugarId.HasValue && vm.LugarId.Value > 0)
            {
                var lugar = await db.Lugares.FirstOrDefaultAsync(l => l.Id == vm.LugarId.Value);
                if (lugar == null)
                {
                    ModelState.AddModelError("LugarId", "El lugar seleccionado no existe.");
                    return;
                }

                vm.Tipo = "Lugar";
                vm.ComercioId = null;
                vm.EventoId = null;
                return;
            }

            ModelState.AddModelError("", "Debes seleccionar un comercio, evento o lugar.");
        }

        private object ConstruirRouteValues(ResenaCreateVM vm)
        {
            if (vm == null)
                return null;

            if (vm.ComercioId.HasValue)
                return new { comercioId = vm.ComercioId.Value };

            if (vm.EventoId.HasValue)
                return new { eventoId = vm.EventoId.Value };

            if (vm.LugarId.HasValue)
                return new { lugarId = vm.LugarId.Value };

            return null;
        }

        private async Task<string> ObtenerNombreElementoAsync(int? comercioId, int? eventoId, int? lugarId)
        {
            if (comercioId.HasValue)
            {
                var comercio = await db.Comercios.FirstOrDefaultAsync(c => c.Id == comercioId.Value);
                return comercio != null ? comercio.Nombre : "Comercio";
            }

            if (eventoId.HasValue)
            {
                var evento = await db.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId.Value);
                return evento != null ? evento.Nombre : "Evento";
            }

            if (lugarId.HasValue)
            {
                var lugar = await db.Lugares.FirstOrDefaultAsync(l => l.Id == lugarId.Value);
                return lugar != null ? lugar.Nombre : "Lugar";
            }

            return null;
        }

        private string ObtenerErroresModelo()
        {
            var errores = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!errores.Any())
                return "No fue posible procesar la reseña.";

            return string.Join(" ", errores);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class ResenaCreateVM
    {
        public int? LugarId { get; set; }
        public int? ComercioId { get; set; }
        public int? EventoId { get; set; }

        [Required]
        public string Tipo { get; set; }

        [Range(1, 5)]
        public int Calificacion { get; set; }

        [StringLength(1000)]
        public string Comentario { get; set; }

        public IEnumerable<HttpPostedFileBase> Imagenes { get; set; }
    }
}