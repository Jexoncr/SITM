using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;
using static turistico.Controllers.AdminController;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsuariosController : Controller
    {
        // =============================
        // USER MANAGER
        // =============================
        private UserManager<ApplicationUser> UserManager =>
            HttpContext.GetOwinContext()
            .GetUserManager<UserManager<ApplicationUser>>();

        private RoleManager<IdentityRole> RoleManager =>
            HttpContext.GetOwinContext()
            .Get<RoleManager<IdentityRole>>();

        // =============================
        // LISTADO USUARIOS
        // =============================
        public async Task<ActionResult> Index(string q = "")
        {
            q = (q ?? "").Trim().ToLower();

            var users = UserManager.Users.ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                users = users.Where(u =>
                    (u.Email ?? "").ToLower().Contains(q) ||
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
                    Roles = roles.ToList(),
                    Bloqueado =
                        u.LockoutEndDateUtc.HasValue &&
                        u.LockoutEndDateUtc > DateTime.UtcNow
                });
            }

            ViewBag.Query = q;
            return View(vm);
        }

        // =============================
        // EDITAR USUARIO (GET)
        // =============================
        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return HttpNotFound();

            var userRoles = await UserManager.GetRolesAsync(user.Id);
            var allRoles = RoleManager.Roles.ToList();

            var vm = new EditUsuarioVM
            {
                UserId = user.Id,
                Email = user.Email,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                PhoneNumber = user.PhoneNumber,
                Bloqueado =
                    user.LockoutEndDateUtc.HasValue &&
                    user.LockoutEndDateUtc > DateTime.UtcNow,

                Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.Name,
                    Assigned = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(vm);
        }

        // =============================
        // EDITAR USUARIO (POST)
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditUsuarioVM vm)
        {
            var user = await UserManager.FindByIdAsync(vm.UserId);
            if (user == null) return HttpNotFound();

            // actualizar datos
            user.Nombre = vm.Nombre;
            user.Apellido = vm.Apellido;
            user.PhoneNumber = vm.PhoneNumber;

            await UserManager.UpdateAsync(user);

            // =============================
            // ROLES
            // =============================
            var currentRoles = await UserManager.GetRolesAsync(user.Id);

            var selectedRoles = vm.Roles
                .Where(r => r.Assigned)
                .Select(r => r.RoleName)
                .ToList();

            var removeRoles = currentRoles.Except(selectedRoles).ToArray();
            var addRoles = selectedRoles.Except(currentRoles).ToArray();

            if (removeRoles.Any())
                await UserManager.RemoveFromRolesAsync(user.Id, removeRoles);

            if (addRoles.Any())
                await UserManager.AddToRolesAsync(user.Id, addRoles);

            // =============================
            // BLOQUEAR / DESBLOQUEAR
            // =============================
            await UserManager.SetLockoutEnabledAsync(user.Id, true);

            if (vm.Bloqueado)
            {
                await UserManager.SetLockoutEndDateAsync(
                    user.Id,
                    DateTimeOffset.UtcNow.AddYears(100)
                );
            }
            else
            {
                await UserManager.SetLockoutEndDateAsync(
                    user.Id,
                    DateTimeOffset.UtcNow
                );
            }

            TempData["Ok"] = "Usuario actualizado correctamente";
            return RedirectToAction("Index");
        }

        // =============================
        // ELIMINAR USUARIO
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return HttpNotFound();

            await UserManager.DeleteAsync(user);

            TempData["Ok"] = "Usuario eliminado";
            return RedirectToAction("Index");
        }

        // =============================
        // CREAR USUARIO (GET)
        // =============================
        public ActionResult Create()
        {
            // roles disponibles para asignar al crear
            var allRoles = RoleManager.Roles.OrderBy(r => r.Name).ToList();

            var vm = new CreateUsuarioVM
            {
                Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.Name,
                    Assigned = (r.Name == "Cliente") // por defecto Cliente
                }).ToList()
            };

            return View(vm);
        }

        // =============================
        // CREAR USUARIO (POST)
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateUsuarioVM vm)
        {
            if (!ModelState.IsValid)
            {
                // recargar roles por si hubo error
                var allRoles = RoleManager.Roles.OrderBy(r => r.Name).ToList();
                vm.Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.Name,
                    Assigned = vm.Roles?.Any(x => x.RoleName == r.Name && x.Assigned) == true
                }).ToList();

                return View(vm);
            }

            // Validar correo duplicado
            var exists = await UserManager.FindByEmailAsync(vm.Email);
            if (exists != null)
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese correo.");
                // recargar roles
                var allRoles = RoleManager.Roles.OrderBy(r => r.Name).ToList();
                vm.Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.Name,
                    Assigned = vm.Roles?.Any(x => x.RoleName == r.Name && x.Assigned) == true
                }).ToList();
                return View(vm);
            }

            // Crear user
            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                Nombre = vm.Nombre,
                Apellido = vm.Apellido,
                PhoneNumber = vm.PhoneNumber
            };

            var result = await UserManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err);

                // recargar roles
                var allRoles = RoleManager.Roles.OrderBy(r => r.Name).ToList();
                vm.Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.Name,
                    Assigned = vm.Roles?.Any(x => x.RoleName == r.Name && x.Assigned) == true
                }).ToList();

                return View(vm);
            }

            // Asignar roles seleccionados (mínimo 1)
            var selectedRoles = (vm.Roles ?? new List<RoleCheckVM>())
                .Where(r => r.Assigned)
                .Select(r => r.RoleName)
                .ToArray();

            if (selectedRoles.Length == 0)
                selectedRoles = new[] { "Cliente" };

            await UserManager.AddToRolesAsync(user.Id, selectedRoles);

            // Lockout opcional
            await UserManager.SetLockoutEnabledAsync(user.Id, true);
            if (vm.Bloqueado)
            {
                await UserManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.UtcNow.AddYears(100));
            }

            TempData["Ok"] = "Usuario creado correctamente.";
            return RedirectToAction("Index");
        }
    }



    // =============================
    // VIEW MODELS
    // =============================
    public class UsuarioRowVM
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool Bloqueado { get; set; }
    }

    public class EditUsuarioVM
    {
        public string UserId { get; set; }
        public string Email { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string PhoneNumber { get; set; }

        public bool Bloqueado { get; set; }

        public List<RoleCheckVM> Roles { get; set; }
    }

    public class RoleCheckVM
    {
        public string RoleName { get; set; }
        public bool Assigned { get; set; }
    }


}


