using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Spatial;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminComerciosController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private DbGeography ConstruirUbicacion(decimal? latitud, decimal? longitud)
        {
            if (!latitud.HasValue || !longitud.HasValue)
                return null;

            return DbGeography.FromText(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "POINT({0} {1})",
                    longitud.Value,
                    latitud.Value
                ),
                4326
            );
        }

        private void NormalizarUbicacionMapa(ComercioAdminVM vm)
        {
            ModelState.Remove("MapaLatitud");
            ModelState.Remove("MapaLongitud");

            var latRaw = (Request["MapaLatitud"] ?? "").Trim().Replace(",", ".");
            var lngRaw = (Request["MapaLongitud"] ?? "").Trim().Replace(",", ".");

            decimal lat;
            decimal lng;

            if (string.IsNullOrWhiteSpace(latRaw) || string.IsNullOrWhiteSpace(lngRaw))
            {
                ModelState.AddModelError("", "Debes seleccionar la ubicación del comercio en el mapa.");
                return;
            }

            if (!decimal.TryParse(latRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lat))
            {
                ModelState.AddModelError("MapaLatitud", "Latitud del mapa inválida.");
                return;
            }

            if (!decimal.TryParse(lngRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lng))
            {
                ModelState.AddModelError("MapaLongitud", "Longitud del mapa inválida.");
                return;
            }

            if (lat < -90 || lat > 90)
                ModelState.AddModelError("MapaLatitud", "La latitud del mapa debe estar entre -90 y 90.");

            if (lng < -180 || lng > 180)
                ModelState.AddModelError("MapaLongitud", "La longitud del mapa debe estar entre -180 y 180.");

            vm.MapaLatitud = lat;
            vm.MapaLongitud = lng;
        }

        private async Task<List<SelectListItem>> ObtenerUsuariosComercioDisponiblesAsync(string selectedUserId = null, int? comercioActualId = null)
        {
            var roleComercio = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Comercio");
            if (roleComercio == null)
                return new List<SelectListItem>();

            var usuariosComercio = await db.Users
                .Where(u => u.Roles.Any(r => r.RoleId == roleComercio.Id))
                .OrderBy(u => u.Email)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Nombre,
                    u.Apellido
                })
                .ToListAsync();

            var usuariosAsignados = await db.Comercios
                .Where(c => c.UserId != null && (!comercioActualId.HasValue || c.Id != comercioActualId.Value))
                .Select(c => c.UserId)
                .ToListAsync();

            return usuariosComercio
                .Where(u => !usuariosAsignados.Contains(u.Id) || u.Id == selectedUserId)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = string.Format(
                        "{0} ({1})",
                        string.IsNullOrWhiteSpace((u.Nombre + " " + u.Apellido).Trim())
                            ? u.Email
                            : (u.Nombre + " " + u.Apellido).Trim(),
                        u.Email),
                    Selected = u.Id == selectedUserId
                })
                .ToList();
        }

        private async Task CargarViewBagsAsync(int? categoriaId = null, string userId = null, int? comercioActualId = null)
        {
            ViewBag.Categorias = new SelectList(
                await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                "Id",
                "Nombre",
                categoriaId
            );

            var usuariosComercio = await ObtenerUsuariosComercioDisponiblesAsync(userId, comercioActualId);
            ViewBag.UsuariosComercio = new SelectList(usuariosComercio, "Value", "Text", userId);
        }

        private async Task GuardarImagenLugarAsync(HttpPostedFileBase imagen, int lugarId, bool reemplazarExistentes = false)
        {
            if (imagen == null || imagen.ContentLength <= 0)
                return;

            var ext = Path.GetExtension(imagen.FileName)?.ToLowerInvariant();
            var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!permitidas.Contains(ext))
                return;

            if (imagen.ContentLength > 5 * 1024 * 1024)
                return;

            var carpeta = Server.MapPath("~/Content/img/lugares");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            if (reemplazarExistentes)
            {
                var anteriores = await db.ImagenesLugar
                    .Where(i => i.LugarId == lugarId)
                    .ToListAsync();

                foreach (var img in anteriores)
                    db.ImagenesLugar.Remove(img);

                await db.SaveChangesAsync();
            }

            var nombreArchivo = Guid.NewGuid().ToString("N") + ext;
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);
            imagen.SaveAs(rutaFisica);

            db.ImagenesLugar.Add(new ImagenLugar
            {
                LugarId = lugarId,
                UrlImagen = "/Content/img/lugares/" + nombreArchivo
            });

            await db.SaveChangesAsync();
        }

        public async Task<ActionResult> Index(string q = "", string estado = "", int pagina = 1)
        {
            const int pageSize = 6;

            q = (q ?? "").Trim();
            estado = (estado ?? "").Trim();

            var query = db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.Lugar.Categoria)
                .Include(c => c.Lugar.ImagenesLugar)
                .Include(c => c.ComercioRegulado)
                .Include(c => c.User)
                .Include(c => c.Eventos)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();
                query = query.Where(c =>
                    (c.Nombre ?? "").ToLower().Contains(qq) ||
                    (c.Descripcion ?? "").ToLower().Contains(qq) ||
                    (c.Lugar.Direccion ?? "").ToLower().Contains(qq) ||
                    (c.Lugar.Categoria.Nombre ?? "").ToLower().Contains(qq) ||
                    (c.User.Email ?? "").ToLower().Contains(qq) ||
                    (c.User.Nombre ?? "").ToLower().Contains(qq) ||
                    (c.User.Apellido ?? "").ToLower().Contains(qq));
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(c => c.Lugar.Estado == estado);
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Nombre)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new PaginacionVM<Comercio>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling((double)totalRegistros / pageSize),
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = pageSize
            };

            ViewBag.Q = q;
            ViewBag.Estado = estado;

            return View(vm);
        }

        public async Task<ActionResult> Create()
        {
            await CargarViewBagsAsync();
            return View(new ComercioAdminVM
            {
                Estado = "Aprobado"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ComercioAdminVM vm)
        {
            NormalizarUbicacionMapa(vm);

            if (!string.IsNullOrWhiteSpace(vm.UserId))
            {
                var yaAsignado = await db.Comercios.AnyAsync(c => c.UserId == vm.UserId);
                if (yaAsignado)
                    ModelState.AddModelError("UserId", "Ese usuario comercio ya está asignado a otro comercio.");
            }

            if (!ModelState.IsValid)
            {
                await CargarViewBagsAsync(vm.CategoriaId, vm.UserId);
                TempData["Err"] = "Revisa los datos del comercio.";
                return View(vm);
            }

            var lugar = new Lugar
            {
                CategoriaId = vm.CategoriaId,
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion,
                Direccion = vm.Direccion,
                DireccionMapa = vm.DireccionMapa,
                Telefono = vm.Telefono,
                Horario = vm.Horario,
                SitioWeb = vm.SitioWeb,
                Estado = string.IsNullOrWhiteSpace(vm.Estado) ? "Aprobado" : vm.Estado,
                Ubicacion = ConstruirUbicacion(vm.MapaLatitud, vm.MapaLongitud)
            };

            db.Lugares.Add(lugar);
            await db.SaveChangesAsync();

            var comercio = new Comercio
            {
                LugarId = lugar.Id,
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion,
                Telefono = vm.Telefono,
                LinkWhatsApp = vm.LinkWhatsApp,
                UserId = string.IsNullOrWhiteSpace(vm.UserId) ? null : vm.UserId
            };

            db.Comercios.Add(comercio);
            await db.SaveChangesAsync();

            if (vm.EsRegulado)
            {
                db.ComerciosRegulados.Add(new ComercioRegulado
                {
                    ComercioId = comercio.Id,
                    NumeroPatente = vm.NumeroPatente,
                    FechaVencimiento = vm.FechaVencimiento,
                    EstadoValidacion = "Aprobado"
                });

                await db.SaveChangesAsync();
            }

            await GuardarImagenLugarAsync(vm.Imagen, lugar.Id, false);

            TempData["Ok"] = "Comercio creado correctamente.";
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.ComercioRegulado)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null)
                return HttpNotFound();

            await CargarViewBagsAsync(comercio.Lugar.CategoriaId, comercio.UserId, id);

            var vm = new ComercioAdminVM
            {
                Nombre = comercio.Nombre,
                Descripcion = comercio.Descripcion,
                LinkWhatsApp = comercio.LinkWhatsApp,
                CategoriaId = comercio.Lugar.CategoriaId,
                Direccion = comercio.Lugar.Direccion,
                DireccionMapa = comercio.Lugar.DireccionMapa,
                Telefono = comercio.Telefono ?? comercio.Lugar.Telefono,
                Horario = comercio.Lugar.Horario,
                SitioWeb = comercio.Lugar.SitioWeb,
                Estado = comercio.Lugar.Estado,
                UserId = comercio.UserId,
                EsRegulado = comercio.ComercioRegulado != null,
                NumeroPatente = comercio.ComercioRegulado != null ? comercio.ComercioRegulado.NumeroPatente : null,
                FechaVencimiento = comercio.ComercioRegulado != null ? comercio.ComercioRegulado.FechaVencimiento : null,
                MapaLatitud = comercio.Lugar.Ubicacion != null && comercio.Lugar.Ubicacion.Latitude.HasValue
                    ? (decimal?)comercio.Lugar.Ubicacion.Latitude.Value
                    : null,
                MapaLongitud = comercio.Lugar.Ubicacion != null && comercio.Lugar.Ubicacion.Longitude.HasValue
                    ? (decimal?)comercio.Lugar.Ubicacion.Longitude.Value
                    : null
            };

            var img = await db.ImagenesLugar
                .Where(i => i.LugarId == comercio.LugarId)
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            ViewBag.ImagenActual = img != null ? img.UrlImagen : null;
            ViewBag.ComercioId = comercio.Id;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, ComercioAdminVM vm)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.ComercioRegulado)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null)
                return HttpNotFound();

            NormalizarUbicacionMapa(vm);

            if (!string.IsNullOrWhiteSpace(vm.UserId))
            {
                var yaAsignado = await db.Comercios.AnyAsync(c => c.UserId == vm.UserId && c.Id != id);
                if (yaAsignado)
                    ModelState.AddModelError("UserId", "Ese usuario comercio ya está asignado a otro comercio.");
            }

            if (!ModelState.IsValid)
            {
                await CargarViewBagsAsync(vm.CategoriaId, vm.UserId, id);
                ViewBag.ComercioId = id;

                var imgActual = await db.ImagenesLugar
                    .Where(i => i.LugarId == comercio.LugarId)
                    .OrderByDescending(i => i.Id)
                    .FirstOrDefaultAsync();

                ViewBag.ImagenActual = imgActual != null ? imgActual.UrlImagen : null;

                TempData["Err"] = "Revisa los datos del comercio.";
                return View(vm);
            }

            comercio.Lugar.CategoriaId = vm.CategoriaId;
            comercio.Lugar.Nombre = vm.Nombre;
            comercio.Lugar.Descripcion = vm.Descripcion;
            comercio.Lugar.Direccion = vm.Direccion;
            comercio.Lugar.DireccionMapa = vm.DireccionMapa;
            comercio.Lugar.Telefono = vm.Telefono;
            comercio.Lugar.Horario = vm.Horario;
            comercio.Lugar.SitioWeb = vm.SitioWeb;
            comercio.Lugar.Estado = vm.Estado;
            comercio.Lugar.Ubicacion = ConstruirUbicacion(vm.MapaLatitud, vm.MapaLongitud);

            comercio.Nombre = vm.Nombre;
            comercio.Descripcion = vm.Descripcion;
            comercio.Telefono = vm.Telefono;
            comercio.LinkWhatsApp = vm.LinkWhatsApp;
            comercio.UserId = string.IsNullOrWhiteSpace(vm.UserId) ? null : vm.UserId;

            if (vm.EsRegulado)
            {
                if (comercio.ComercioRegulado == null)
                {
                    db.ComerciosRegulados.Add(new ComercioRegulado
                    {
                        ComercioId = comercio.Id,
                        NumeroPatente = vm.NumeroPatente,
                        FechaVencimiento = vm.FechaVencimiento,
                        EstadoValidacion = "Aprobado"
                    });
                }
                else
                {
                    comercio.ComercioRegulado.NumeroPatente = vm.NumeroPatente;
                    comercio.ComercioRegulado.FechaVencimiento = vm.FechaVencimiento;
                    comercio.ComercioRegulado.EstadoValidacion = "Aprobado";
                }
            }
            else
            {
                if (comercio.ComercioRegulado != null)
                    db.ComerciosRegulados.Remove(comercio.ComercioRegulado);
            }

            await db.SaveChangesAsync();
            await GuardarImagenLugarAsync(vm.Imagen, comercio.LugarId, true);

            TempData["Ok"] = "Comercio actualizado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Aprobar(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null)
            {
                TempData["Err"] = "No se encontró el comercio.";
                return RedirectToAction("Index");
            }

            comercio.Lugar.Estado = "Aprobado";
            await db.SaveChangesAsync();

            TempData["Ok"] = "Comercio aprobado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Rechazar(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null)
            {
                TempData["Err"] = "No se encontró el comercio.";
                return RedirectToAction("Index");
            }

            comercio.Lugar.Estado = "Rechazado";
            await db.SaveChangesAsync();

            TempData["Ok"] = "Comercio rechazado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.ComercioRegulado)
                .Include(c => c.Eventos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null)
            {
                TempData["Err"] = "No se encontró el comercio.";
                return RedirectToAction("Index");
            }

            var tieneUsuario = !string.IsNullOrWhiteSpace(comercio.UserId);
            var tieneEventos = comercio.Eventos != null && comercio.Eventos.Any();

            if (tieneUsuario || tieneEventos)
            {
                TempData["Err"] = "No se puede eliminar el comercio porque tiene un usuario asignado o eventos relacionados.";
                return RedirectToAction("Index");
            }

            try
            {
                var imagenes = await db.ImagenesLugar
                    .Where(i => i.LugarId == comercio.LugarId)
                    .ToListAsync();

                foreach (var img in imagenes)
                    db.ImagenesLugar.Remove(img);

                if (comercio.ComercioRegulado != null)
                    db.ComerciosRegulados.Remove(comercio.ComercioRegulado);

                var lugar = comercio.Lugar;

                db.Comercios.Remove(comercio);

                if (lugar != null)
                    db.Lugares.Remove(lugar);

                await db.SaveChangesAsync();
                TempData["Ok"] = "Comercio eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Err"] = "No se puede eliminar el comercio porque tiene información relacionada en el sistema.";
            }
            catch (Exception)
            {
                TempData["Err"] = "Ocurrió un problema al eliminar el comercio.";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class ComercioAdminVM
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string LinkWhatsApp { get; set; }
        public int CategoriaId { get; set; }
        public HttpPostedFileBase Imagen { get; set; }
        public string Direccion { get; set; }
        public string DireccionMapa { get; set; }
        public string Telefono { get; set; }
        public string Horario { get; set; }
        public string SitioWeb { get; set; }
        public string UserId { get; set; }
        public decimal? MapaLatitud { get; set; }
        public decimal? MapaLongitud { get; set; }
        public string Estado { get; set; }
        public bool EsRegulado { get; set; }
        public string NumeroPatente { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}