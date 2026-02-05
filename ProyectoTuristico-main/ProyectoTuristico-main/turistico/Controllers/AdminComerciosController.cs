using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminComerciosController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

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

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(
                    await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(),
                    "Id", "Nombre", vm.CategoriaId
                );
                ViewBag.ComercioId = id;
                return View(vm);
            }

            // Lugar
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

            // Comercio
            comercio.Nombre = vm.Nombre;
            comercio.Descripcion = vm.Descripcion;

            // Regulado
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
                {
                    db.ComerciosRegulados.Remove(comercio.ComercioRegulado);
                }
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
                .Include(c => c.Lugar)
                .Include(c => c.ComercioRegulado)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comercio == null) return HttpNotFound();

            if (comercio.ComercioRegulado != null)
                db.ComerciosRegulados.Remove(comercio.ComercioRegulado);

            // IMPORTANTE: primero se elimina el Comercio y luego el Lugar
            db.Comercios.Remove(comercio);
            db.Lugares.Remove(comercio.Lugar);

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
}








