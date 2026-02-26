using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;
using System.Data.Entity;
using System.Linq;
using System.Collections.Generic;


namespace turistico.Controllers
{
    public class HomeController : Controller
    {
        // Páginas públicas
        public ActionResult Index() => View();

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

        public ActionResult Eventos() => View();
        public async Task<ActionResult> Comercios()
        {
            using (var db = new ApplicationDbContext())
            {
                var model = await db.Comercios
                    .Include(c => c.Lugar)
                    .Include(c => c.Lugar.Categoria)
                    .Include(c => c.Lugar.ImagenesLugar)
                    .Where(c => c.Lugar.Estado == "Aprobado")
                    .OrderBy(c => c.Nombre)
                    .Select(c => new ComercioDTO
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Descripcion = c.Descripcion,
                        LinkWhatsApp = c.LinkWhatsApp,  // ← ESTA ES LA LÍNEA QUE FALTABA
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
        public ActionResult Resenas() => View();

        public async Task<ActionResult> Mapa()
        {
            using (var db = new ApplicationDbContext())
            {
                // 1) ids de Lugares que son Comercios (para habilitar "Ver detalle")
                var comercioLugarIds = await db.Comercios
                    .Select(c => c.LugarId)
                    .ToListAsync();

                // 2) Lugares aprobados/activos para el mapa
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

                // 3) Categorías desde BD (para generar filtros dinámicos)
                var categorias = await db.Categorias
                    .OrderBy(c => c.Nombre)
                    .Select(c => c.Nombre)
                    .ToListAsync();

                // Si querés que SOLO salgan las categorías que tienen lugares:
                // categorias = lugares.Select(x => x.Categoria).Distinct().OrderBy(x => x).ToList();

                var model = new MapaTuristicoVM
                {
                    Categorias = categorias,
                    Lugares = lugares
                };

                return View(model);
            }
        }

        public ActionResult Recuperar() => View();

        
       
       

       

        // LOGIN (GET)
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // LOGIN (POST) 
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model, string returnUrl)
        {
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // REGISTER (GET) 
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return RedirectToAction("Register", "Account");
        }

        // REGISTER (POST) -> redirige a Account/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            return RedirectToAction("Register", "Account");
        }

        // LOGOUT (POST) -> redirige a Account/Logout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            return RedirectToAction("Logout", "Account");
        }

        
        //Páginas privadas
        

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

            // Actualizar campos permitidos
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
        public ActionResult MisReservas() => View();
    }
    public class MapaVM
    {
        public List<Categoria> Categorias { get; set; }
        public List<MapaItemVM> Items { get; set; }
    }

    public class MapaItemVM
    {
        public string Tipo { get; set; }      // "Comercio" o "Lugar"
        public int Id { get; set; }           // Id del comercio (si aplica) o del lugar
        public int LugarId { get; set; }      // Lugar.Id
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; } // Nombre de la categoría
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }
}
