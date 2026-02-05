using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;
using System.Data.Entity;
using System.Linq;


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
                var comercios = await db.Comercios
                    .Include(c => c.Lugar)
                    .Include(c => c.Lugar.Categoria)
                    .Where(c => c.Lugar.Estado == "Aprobado")
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                return View(comercios);
            }
        }
        public ActionResult Resenas() => View();
        public ActionResult Mapa() => View();
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
}
