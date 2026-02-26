using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<ApplicationUser> UserManager =>
            HttpContext.GetOwinContext().GetUserManager<UserManager<ApplicationUser>>();

        private IAuthenticationManager Auth =>
            HttpContext.GetOwinContext().Authentication;

        // ✅ LOGIN (GET)
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // ✅ LOGIN (POST)
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginVM model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await UserManager.FindAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            Auth.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Auth.SignIn(new AuthenticationProperties { IsPersistent = model.RememberMe }, identity);

            var isAdmin = await UserManager.IsInRoleAsync(user.Id, "Admin");

            // ✅ Si es Admin y el returnUrl es vacío, "/" o apunta a Home => Dashboard Admin
            if (isAdmin && (
                string.IsNullOrWhiteSpace(returnUrl) ||
                returnUrl == "/" ||
                returnUrl.StartsWith("/Home", System.StringComparison.OrdinalIgnoreCase)
            ))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            // ✅ Si venía rebotado por [Authorize], respeta ReturnUrl
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // ✅ Fallback por rol
            return isAdmin
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Home");
        }

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
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                PhoneNumber = model.PhoneNumber
            };

            var result = await UserManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err);

                return View(model);
            }

            // ✅ Rol por defecto
            await UserManager.AddToRoleAsync(user.Id, "Cliente");

            // ✅ Auto login tras registrarse
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            Auth.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Auth.SignIn(new AuthenticationProperties { IsPersistent = false }, identity);

            return RedirectToAction("Index", "Home");
        }

        // ✅ LOGOUT (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Auth.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }
    }
}
