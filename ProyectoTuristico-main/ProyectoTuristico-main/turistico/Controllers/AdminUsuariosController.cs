using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsuariosController : Controller
    {
        private UserManager<ApplicationUser> UserManager =>
            HttpContext.GetOwinContext().GetUserManager<UserManager<ApplicationUser>>();

        private RoleManager<IdentityRole> RoleManager =>
            HttpContext.GetOwinContext().Get<RoleManager<IdentityRole>>();

        public async Task<ActionResult> Index(string q = "")
        {
            q = (q ?? "").Trim().ToLower();

            var users = UserManager.Users.ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                users = users.Where(u =>
                    (u.Email ?? "").ToLower().Contains(q) ||
                    (u.UserName ?? "").ToLower().Contains(q) ||
                    (u.Nombre ?? "").ToLower().Contains(q) ||
                    (u.Apellido ?? "").ToLower().Contains(q)
                ).ToList();
            }

            var vm = new List<UsuarioRowVM>();
            foreach (var u in users.OrderBy(x => x.Email))
            {
                var roles = await UserManager.GetRolesAsync(u.Id);
                vm.Add(new UsuarioRowVM
                {
                    Id = u.Id,
                    Email = u.Email,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    PhoneNumber = u.PhoneNumber,
                    Roles = roles.ToList()
                });
            }

            ViewBag.Query = q;
            return View(vm);
        }

        // GET: AdminUsuarios/EditRoles/{id}
        public async Task<ActionResult> EditRoles(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return HttpNotFound();

            var allRoles = RoleManager.Roles.OrderBy(r => r.Name).ToList();
            var userRoles = await UserManager.GetRolesAsync(user.Id);

            var vm = new EditUserRolesVM
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.Name,
                    Assigned = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditRoles(EditUserRolesVM vm)
        {
            var user = await UserManager.FindByIdAsync(vm.UserId);
            if (user == null) return HttpNotFound();

            var current = await UserManager.GetRolesAsync(user.Id);
            var desired = (vm.Roles ?? new List<RoleCheckVM>())
                .Where(x => x.Assigned)
                .Select(x => x.RoleName)
                .ToList();

            // No permitir dejar el sistema sin Admin si este usuario era el único (opcional)
            // (Para simple: solo protegemos que el usuario admin@turistico.com no pierda Admin)
            if ((user.Email ?? "").ToLower() == "admin@turistico.com" && !desired.Contains("Admin"))
            {
                TempData["Err"] = "No podés quitar el rol Admin a admin@turistico.com.";
                return RedirectToAction("EditRoles", new { id = user.Id });
            }

            var remove = current.Except(desired).ToArray();
            var add = desired.Except(current).ToArray();

            if (remove.Any())
                await UserManager.RemoveFromRolesAsync(user.Id, remove);

            if (add.Any())
                await UserManager.AddToRolesAsync(user.Id, add);

            TempData["Ok"] = "Roles actualizados.";
            return RedirectToAction("Index");
        }
    }

    public class UsuarioRowVM
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class EditUserRolesVM
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<RoleCheckVM> Roles { get; set; } = new List<RoleCheckVM>();
    }

    public class RoleCheckVM
    {
        public string RoleName { get; set; }
        public bool Assigned { get; set; }
    }
}
