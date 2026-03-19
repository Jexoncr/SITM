using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    public class HomeController : Controller
    {
       
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public ActionResult Eventos()
        {
            return RedirectToAction("Index", "Eventos");
        }

        public async Task<ActionResult> Comercios()
        {
            using (var db = new ApplicationDbContext())
            {
                var model = await db.Comercios
                    .Include(c => c.Lugar)
                    .Include(c => c.Lugar.Categoria)
                    .Include(c => c.Lugar.ImagenesLugar)
                    .OrderBy(c => c.Nombre)
                    .Select(c => new ComercioDTO
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Descripcion = c.Descripcion,
                        LinkWhatsApp = c.LinkWhatsApp,
                        Categoria = c.Lugar.Categoria.Nombre,
                        Direccion = c.Lugar.Direccion,
                        Ubicacion = c.Lugar.Direccion,
                        Telefono = c.Lugar.Telefono,
                        Horario = c.Lugar.Horario,
                        SitioWeb = c.Lugar.SitioWeb,
                        ImagenUrl = c.Lugar.ImagenesLugar
                            .Select(i => i.UrlImagen)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return View(model);
            }
        }

        public ActionResult Resenas()
        {
            return RedirectToAction("Index", "Resenas");
        }

        public async Task<ActionResult> Mapa()
        {
            using (var db = new ApplicationDbContext())
            {
                var comercioLugarIds = await db.Comercios
                    .Select(c => c.LugarId)
                    .ToListAsync();

                var lugaresEnt = await db.Lugares
                    .Include(l => l.Categoria)
                    .Include(l => l.ImagenesLugar)
                    .Where(l => l.Estado == "Aprobado" || l.Estado == "Activo" || l.Estado == null)
                    .OrderBy(l => l.Nombre)
                    .ToListAsync();

                var lugares = lugaresEnt.Select(l => new MapaLugarDTO
                {
                    Id = l.Id,
                    Nombre = l.Nombre,
                    Descripcion = l.Descripcion,
                    Categoria = l.Categoria != null ? l.Categoria.Nombre : "Sin categoría",
                    Latitud = l.Latitud,
                    Longitud = l.Longitud,
                    Direccion = l.Direccion,
                    ImagenUrl = l.ImagenesLugar.Select(i => i.UrlImagen).FirstOrDefault(),
                    EsComercio = comercioLugarIds.Contains(l.Id)
                }).ToList();

                var categorias = await db.Categorias
                    .OrderBy(c => c.Nombre)
                    .Select(c => c.Nombre)
                    .ToListAsync();

                var model = new MapaTuristicoVM
                {
                    Categorias = categorias,
                    Lugares = lugares
                };

                return View(model);
            }
        }

        public ActionResult Recuperar()
        {
            return View();
        }

        // =========================
        // Login / Register / Logout
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model, string returnUrl)
        {
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return RedirectToAction("Register", "Account");
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            return RedirectToAction("Register", "Account");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            return RedirectToAction("Logout", "Account");
        }

        // =========================
        // Páginas privadas
        // =========================
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Perfil()
        {
            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<UserManager<ApplicationUser>>();

            var userId = User.Identity.GetUserId();
            var user = await userManager.FindByIdAsync(userId);

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Perfil(ApplicationUser model)
        {
            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<UserManager<ApplicationUser>>();

            var userId = User.Identity.GetUserId();
            var user = await userManager.FindByIdAsync(userId);

            user.Nombre = model.Nombre;
            user.Apellido = model.Apellido;
            user.Canton = model.Canton;
            user.PhoneNumber = model.PhoneNumber;

            user.PrefEventos = model.PrefEventos;
            user.PrefEcologico = model.PrefEcologico;
            user.PrefGastronomia = model.PrefGastronomia;
            user.PrefAventura = model.PrefAventura;

            user.TipoNotificacion = model.TipoNotificacion;
            user.Idioma = model.Idioma;

            await userManager.UpdateAsync(user);

            ViewBag.Success = "Perfil actualizado correctamente.";
            return View(user);
        }

        [Authorize]
        public ActionResult MisReservas()
        {
            return RedirectToAction("Index", "Reservas");
        }
    }

    public class MapaVM
    {
        public List<Categoria> Categorias { get; set; }
        public List<MapaItemVM> Items { get; set; }
    }

    public class MapaItemVM
    {
        public string Tipo { get; set; }
        public int Id { get; set; }
        public int LugarId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }
}