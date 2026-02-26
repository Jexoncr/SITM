using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using turistico.Models;
using System.Globalization;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminComerciosController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================
        // ✅ FIX: Latitud/Longitud con coma o punto
        // ============================
        private decimal? ParseDecimalFlexible(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim();

            decimal value;
            var styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands;

            // 1) Cultura actual (por si tu app ya está en es-CR)
            if (decimal.TryParse(raw, styles, CultureInfo.CurrentCulture, out value))
                return value;

            // 2) es-CR explícito
            if (decimal.TryParse(raw, styles, new CultureInfo("es-CR"), out value))
                return value;

            // 3) Invariante (punto decimal)
            if (decimal.TryParse(raw, styles, CultureInfo.InvariantCulture, out value))
                return value;

            // 4) Heurística: si viene "1.234,56" => quitar miles y poner punto decimal
            if (raw.Contains(".") && raw.Contains(","))
            {
                var normalized = raw.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out value))
                    return value;
            }

            // 5) Si viene solo con coma "10,469956" => cambiar coma por punto
            if (raw.Contains(",") && !raw.Contains("."))
            {
                var normalized = raw.Replace(",", ".");
                if (decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out value))
                    return value;
            }

            return null;
        }

        private void NormalizarLatLng(ComercioAdminVM vm)
        {
            // Ojo: estos nombres deben coincidir con los "name" de tus inputs (normalmente Latitud/Longitud)
            var latRaw = (Request["Latitud"] ?? "").Trim();
            var lngRaw = (Request["Longitud"] ?? "").Trim();

            // Si el binder falló, ModelState queda con error y aunque asignés vm.Latitud luego, sigue inválido.
            // Por eso removemos esos 2 campos y los validamos nosotros.
            ModelState.Remove("Latitud");
            ModelState.Remove("Longitud");

            if (!string.IsNullOrWhiteSpace(latRaw))
            {
                var parsedLat = ParseDecimalFlexible(latRaw);
                if (parsedLat.HasValue)
                    vm.Latitud = parsedLat.Value;
                else
                    ModelState.AddModelError("Latitud", "Latitud inválida. Ejemplo válido: 10.469956 (o 10,469956).");
            }

            if (!string.IsNullOrWhiteSpace(lngRaw))
            {
                var parsedLng = ParseDecimalFlexible(lngRaw);
                if (parsedLng.HasValue)
                    vm.Longitud = parsedLng.Value;
                else
                    ModelState.AddModelError("Longitud", "Longitud inválida. Ejemplo válido: -84.469296 (o -84,469296).");
            }

            // Mensaje general (para que NO quede el cuadro rojo vacío)
            if (ModelState.ContainsKey("Latitud") && ModelState["Latitud"].Errors.Any()
                || ModelState.ContainsKey("Longitud") && ModelState["Longitud"].Errors.Any())
            {
                ModelState.AddModelError("", "Revisá Latitud y Longitud (pueden ir con coma o con punto).");
            }
        }

        // LISTA
        public async Task<ActionResult> Index()
        {
            var data = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.Lugar.Categoria)
                .Include(c => c.ComercioRegulado)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(data);
        }

        // CREATE (GET)
        public async Task<ActionResult> Create()
        {
            ViewBag.Categorias = new SelectList(
                await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                "Id", "Nombre"
            );
            return View();
        }

        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ComercioAdminVM vm)
        {
            // ✅ FIX: normalizar antes del IsValid
            NormalizarLatLng(vm);

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(
                    await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                    "Id", "Nombre", vm.CategoriaId
                );
                return View(vm);
            }

            // 1) Lugar
            var lugar = new Lugar
            {
                CategoriaId = vm.CategoriaId,
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion,
                Direccion = vm.Direccion,
                Telefono = vm.Telefono,
                Horario = vm.Horario,
                SitioWeb = vm.SitioWeb,
                Latitud = vm.Latitud,
                Longitud = vm.Longitud,
                Estado = string.IsNullOrWhiteSpace(vm.Estado) ? "Aprobado" : vm.Estado
            };

            db.Lugares.Add(lugar);
            await db.SaveChangesAsync();

            // 2) Comercio
            var comercio = new Comercio
            {
                LugarId = lugar.Id,
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion
            };

            db.Comercios.Add(comercio);
            await db.SaveChangesAsync();

            // 3) Regulado (opcional)
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

            // 4) Imagen (opcional)
            if (vm.Imagen != null && vm.Imagen.ContentLength > 0)
            {
                var ext = System.IO.Path.GetExtension(vm.Imagen.FileName)?.ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("", "Formato de imagen no permitido. Usa jpg, png o webp.");

                    ViewBag.Categorias = new SelectList(
                        await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                        "Id", "Nombre", vm.CategoriaId
                    );

                    return View(vm);
                }

                // (Opcional) tamaño max 5MB
                if (vm.Imagen.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "La imagen es muy pesada (máx. 5MB).");

                    ViewBag.Categorias = new SelectList(
                        await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                        "Id", "Nombre", vm.CategoriaId
                    );

                    return View(vm);
                }

                var fileName = $"{System.Guid.NewGuid()}{ext}";
                var folder = Server.MapPath("~/Content/img/lugares");
                System.IO.Directory.CreateDirectory(folder);

                var physicalPath = System.IO.Path.Combine(folder, fileName);
                vm.Imagen.SaveAs(physicalPath);

                db.ImagenesLugar.Add(new ImagenLugar
                {
                    LugarId = lugar.Id,
                    UrlImagen = "/Content/img/lugares/" + fileName
                });

                await db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // APROBAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Aprobar(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null) return HttpNotFound();

            comercio.Lugar.Estado = "Aprobado";
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // RECHAZAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Rechazar(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null) return HttpNotFound();

            comercio.Lugar.Estado = "Rechazado";
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // EDIT (GET)
        public async Task<ActionResult> Edit(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.ComercioRegulado)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null) return HttpNotFound();

            ViewBag.Categorias = new SelectList(
                await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                "Id", "Nombre",
                comercio.Lugar.CategoriaId
            );

            var vm = new ComercioAdminVM
            {
                Nombre = comercio.Nombre,
                Descripcion = comercio.Descripcion,

                CategoriaId = comercio.Lugar.CategoriaId,
                Direccion = comercio.Lugar.Direccion,
                Telefono = comercio.Lugar.Telefono,
                Horario = comercio.Lugar.Horario,
                SitioWeb = comercio.Lugar.SitioWeb,
                Latitud = comercio.Lugar.Latitud,
                Longitud = comercio.Lugar.Longitud,
                Estado = comercio.Lugar.Estado,

                EsRegulado = comercio.ComercioRegulado != null,
                NumeroPatente = comercio.ComercioRegulado?.NumeroPatente,
                FechaVencimiento = comercio.ComercioRegulado?.FechaVencimiento
            };

            // Imagen actual (para preview)
            var img = await db.ImagenesLugar
                .Where(i => i.LugarId == comercio.LugarId)
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            ViewBag.ImagenActual = img?.UrlImagen;
            ViewBag.ComercioId = comercio.Id;
            return View(vm);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, ComercioAdminVM vm)
        {
            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.ComercioRegulado)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null) return HttpNotFound();

            // ✅ FIX: normalizar antes del IsValid
            NormalizarLatLng(vm);

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(
                    await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                    "Id", "Nombre", vm.CategoriaId
                );
                ViewBag.ComercioId = id;
                return View(vm);
            }

            // ====== Lugar ======
            comercio.Lugar.CategoriaId = vm.CategoriaId;
            comercio.Lugar.Nombre = vm.Nombre;
            comercio.Lugar.Descripcion = vm.Descripcion;
            comercio.Lugar.Direccion = vm.Direccion;
            comercio.Lugar.Telefono = vm.Telefono;
            comercio.Lugar.Horario = vm.Horario;
            comercio.Lugar.SitioWeb = vm.SitioWeb;
            comercio.Lugar.Latitud = vm.Latitud;
            comercio.Lugar.Longitud = vm.Longitud;
            comercio.Lugar.Estado = vm.Estado;

            // ====== Comercio ======
            comercio.Nombre = vm.Nombre;
            comercio.Descripcion = vm.Descripcion;

            // ====== Regulado ======
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
                }
            }
            else
            {
                if (comercio.ComercioRegulado != null)
                    db.ComerciosRegulados.Remove(comercio.ComercioRegulado);
            }

            // ====== Imagen (opcional) ======
            if (vm.Imagen != null && vm.Imagen.ContentLength > 0)
            {
                var ext = System.IO.Path.GetExtension(vm.Imagen.FileName)?.ToLower();
                var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!permitidas.Contains(ext))
                {
                    ModelState.AddModelError("", "Formato de imagen no permitido. Usa jpg, png o webp.");

                    ViewBag.Categorias = new SelectList(
                        await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                        "Id", "Nombre", vm.CategoriaId
                    );
                    ViewBag.ComercioId = id;
                    return View(vm);
                }

                if (vm.Imagen.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "La imagen es muy pesada (máx. 5MB).");

                    ViewBag.Categorias = new SelectList(
                        await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                        "Id", "Nombre", vm.CategoriaId
                    );
                    ViewBag.ComercioId = id;
                    return View(vm);
                }

                var nombreArchivo = System.Guid.NewGuid() + ext;
                var carpeta = Server.MapPath("~/Content/img/lugares");
                System.IO.Directory.CreateDirectory(carpeta);

                var rutaFisica = System.IO.Path.Combine(carpeta, nombreArchivo);
                vm.Imagen.SaveAs(rutaFisica);

                // Reemplazar: borrar imágenes anteriores del lugar
                var anteriores = db.ImagenesLugar.Where(i => i.LugarId == comercio.LugarId);
                db.ImagenesLugar.RemoveRange(anteriores);

                // Insertar nueva imagen
                db.ImagenesLugar.Add(new ImagenLugar
                {
                    LugarId = comercio.LugarId,
                    UrlImagen = "/Content/img/lugares/" + nombreArchivo
                });
            }

            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // DELETE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var comercio = await db.Comercios
                .Include(c => c.ComercioRegulado)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null)
                return HttpNotFound();

            var lugarId = comercio.LugarId;

            // eliminar regulado si existe
            if (comercio.ComercioRegulado != null)
                db.ComerciosRegulados.Remove(comercio.ComercioRegulado);

            // eliminar comercio
            db.Comercios.Remove(comercio);

            // buscar lugar manualmente
            var lugar = await db.Lugares.FirstOrDefaultAsync(l => l.Id == lugarId);

            if (lugar != null)
                db.Lugares.Remove(lugar);

            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    // ViewModel para el formulario Admin
    public class ComercioAdminVM
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public int CategoriaId { get; set; }
        public System.Web.HttpPostedFileBase Imagen { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public string Horario { get; set; }
        public string SitioWeb { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }

        public string Estado { get; set; } // Aprobado, Pendiente, Rechazado

        public bool EsRegulado { get; set; }
        public string NumeroPatente { get; set; }
        public System.DateTime? FechaVencimiento { get; set; }
    }

    public class CreateUsuarioVM
    {
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string PhoneNumber { get; set; }

        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        public bool Bloqueado { get; set; }

        public List<RoleCheckVM> Roles { get; set; } = new List<RoleCheckVM>();
    }
}