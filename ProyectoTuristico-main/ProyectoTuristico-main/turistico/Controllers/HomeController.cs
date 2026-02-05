using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.Web;
using turistico.Models;

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
        public ActionResult Comercios() => View();
        public ActionResult Resenas() => View();
        public ActionResult Mapa() => View();
        public ActionResult Recuperar() => View();

        // ✅ LOGIN (GET)
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // ✅ LOGIN REAL (POST)
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<UserManager<ApplicationUser>>();

            var auth = HttpContext.GetOwinContext().Authentication;

            var user = await userManager.FindAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            var identity = await userManager.CreateIdentityAsync(
                user,
                DefaultAuthenticationTypes.ApplicationCookie
            );

            auth.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            auth.SignIn(new AuthenticationProperties { IsPersistent = model.RememberMe }, identity);

            return RedirectToAction("Index", "Home");
        }

        // ✅ LOGOUT (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            HttpContext.GetOwinContext().Authentication
                .SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            return RedirectToAction("Login", "Home");
        }

        // 🔒 Páginas privadas (si querés que pidan login)
        [Authorize]
        public async Task<ActionResult> Perfil()
        {
            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<UserManager<ApplicationUser>>();

            var userId = User.Identity.GetUserId();
            var user = await userManager.FindByIdAsync(userId);

            // Podés pasar el user directo a la vista o usar un ViewModel.
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

        // ✅ REGISTER (GET)
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterVM());
        }

        // ✅ REGISTER (POST)
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<UserManager<ApplicationUser>>();

            // Crear usuario
            // Crear usuario (guardando Nombre, Apellido y Teléfono)
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                PhoneNumber = model.PhoneNumber
            };


            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);

                return View(model);
            }

            // (Opcional) asignar rol por defecto
            await userManager.AddToRoleAsync(user.Id, "Cliente");

            // Auto login después de registrarse
            var auth = HttpContext.GetOwinContext().Authentication;

            var identity = await userManager.CreateIdentityAsync(
                user,
                DefaultAuthenticationTypes.ApplicationCookie
            );

            auth.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            auth.SignIn(new AuthenticationProperties { IsPersistent = false }, identity);

            return RedirectToAction("Index", "Home");
        }

    }
}
