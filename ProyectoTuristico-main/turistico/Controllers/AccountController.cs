using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private SignInManager<ApplicationUser, string> _signInManager;
        private UserManager<ApplicationUser> _userManager;

        public AccountController()
        {
        }

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser, string> signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public SignInManager<ApplicationUser, string> SignInManager
        {
            get
            {
                return _signInManager ??
                       (_signInManager = HttpContext.GetOwinContext().Get<SignInManager<ApplicationUser, string>>()
                        ?? new SignInManager<ApplicationUser, string>(UserManager, AuthenticationManager));
            }
            private set { _signInManager = value; }
        }

        public UserManager<ApplicationUser> UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<UserManager<ApplicationUser>>();
            }
            private set { _userManager = value; }
        }

        private IAuthenticationManager AuthenticationManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginVM());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginVM model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(model);
            }

            var result = await SignInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                shouldLockout: false
            );

            switch (result)
            {
                case SignInStatus.Success:
                    var refreshedUser = await UserManager.FindByIdAsync(user.Id);

                    if (refreshedUser != null &&
                        refreshedUser.DebeCambiarContrasena &&
                        refreshedUser.ContrasenaTemporalActiva)
                    {
                        TempData["Info"] = "Debes cambiar tu contraseña temporal antes de continuar.";
                        return RedirectToAction("CambiarContrasenaTemporal", new { returnUrl });
                    }

                    return await RedirectByRoleAsync(user.Id, returnUrl);

                case SignInStatus.LockedOut:
                    ModelState.AddModelError("", "La cuenta está bloqueada.");
                    return View(model);

                default:
                    ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                    return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View(new RegisterVM());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                PhoneNumber = model.PhoneNumber,
                DebeCambiarContrasena = false,
                ContrasenaTemporalActiva = false
            };

            var result = await UserManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await UserManager.IsInRoleAsync(user.Id, "Cliente"))
                    await UserManager.AddToRoleAsync(user.Id, "Cliente");

                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);

            return View(model);
        }

        [Authorize]
        public ActionResult CambiarContrasenaTemporal(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new CambiarContrasenaTemporalVM());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CambiarContrasenaTemporal(CambiarContrasenaTemporalVM model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Err"] = "No se encontró el usuario autenticado.";
                return RedirectToAction("Login");
            }

            var result = await UserManager.ChangePasswordAsync(
                userId,
                model.ContrasenaActual,
                model.NuevaContrasena
            );

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err);

                return View(model);
            }

            user.DebeCambiarContrasena = false;
            user.ContrasenaTemporalActiva = false;
            await UserManager.UpdateAsync(user);

            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

            TempData["Ok"] = "Contraseña actualizada correctamente.";

            return await RedirectByRoleAsync(user.Id, returnUrl);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        private async Task<ActionResult> RedirectByRoleAsync(string userId, string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (await UserManager.IsInRoleAsync(userId, "Admin"))
                return RedirectToAction("Dashboard", "Admin");

            if (await UserManager.IsInRoleAsync(userId, "Comercio"))
            {
                using (var db = new ApplicationDbContext())
                {
                    var comercioAsignado = db.Comercios.Any(c => c.UserId == userId);

                    if (comercioAsignado)
                    {
                        return RedirectToAction("Dashboard", "ComercioPanel");
                    }

                    TempData["Info"] = "Tu cuenta es de comercio, pero todavía no tiene un comercio asignado. Por ahora puedes ingresar al portal normal.";
                    return RedirectToAction("Index", "Home");
                }
            }

            return RedirectToAction("Index", "Home");
        }
    }

    public class CambiarContrasenaTemporalVM
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ContrasenaActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContrasena { get; set; }

        [Required(ErrorMessage = "Debes confirmar la nueva contraseña.")]
        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarNuevaContrasena { get; set; }
    }
}