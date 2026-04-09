using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsuariosController : Controller
    {
        private const int PageSize = 8;

        private UserManager<ApplicationUser> UserManager =>
            HttpContext.GetOwinContext().GetUserManager<UserManager<ApplicationUser>>();

        private RoleManager<IdentityRole> RoleManager =>
            HttpContext.GetOwinContext().Get<RoleManager<IdentityRole>>();

        private string GenerarContrasenaTemporal()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "@#$%*-_";
            var all = upper + lower + digits + symbols;

            using (var rng = RandomNumberGenerator.Create())
            {
                var chars = new List<char>
                {
                    upper[GetInt32FromRng(rng, upper.Length)],
                    lower[GetInt32FromRng(rng, lower.Length)],
                    digits[GetInt32FromRng(rng, digits.Length)],
                    symbols[GetInt32FromRng(rng, symbols.Length)]
                };

                for (int i = chars.Count; i < 10; i++)
                {
                    chars.Add(all[GetInt32FromRng(rng, all.Length)]);
                }

                return new string(chars.OrderBy(x => Guid.NewGuid()).ToArray());
            }
        }

        private static int GetInt32FromRng(RandomNumberGenerator rng, int maxExclusive)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            var buffer = new byte[4];
            uint limit = (uint.MaxValue / (uint)maxExclusive) * (uint)maxExclusive;

            while (true)
            {
                rng.GetBytes(buffer);
                uint value = BitConverter.ToUInt32(buffer, 0);
                if (value < limit)
                    return (int)(value % (uint)maxExclusive);
            }
        }

        private async Task<List<RoleCheckVM>> ConstruirRolesAsync(List<RoleCheckVM> actuales = null)
        {
            var allRoles = await Task.FromResult(RoleManager.Roles.OrderBy(r => r.Name).ToList());

            return allRoles.Select(r => new RoleCheckVM
            {
                RoleName = r.Name,
                Assigned = actuales != null && actuales.Any(x => x.RoleName == r.Name && x.Assigned)
            }).ToList();
        }

        private async Task EnsureRolComercioAsync()
        {
            if (!await RoleManager.RoleExistsAsync("Comercio"))
            {
                await RoleManager.CreateAsync(new IdentityRole("Comercio"));
            }
        }

        public async Task<ActionResult> Index(string q = "", int pagina = 1)
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

            var rows = new List<UsuarioRowVM>();

            foreach (var u in users.OrderBy(x => x.Email))
            {
                var roles = await UserManager.GetRolesAsync(u.Id);

                rows.Add(new UsuarioRowVM
                {
                    Id = u.Id,
                    Email = u.Email,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    PhoneNumber = u.PhoneNumber,
                    Roles = roles.ToList(),
                    Bloqueado = u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc > DateTime.UtcNow,
                    DebeCambiarContrasena = u.DebeCambiarContrasena,
                    ContrasenaTemporalActiva = u.ContrasenaTemporalActiva
                });
            }

            var totalRegistros = rows.Count;
            var totalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize);

            if (totalPaginas == 0)
                totalPaginas = 1;

            if (pagina < 1)
                pagina = 1;

            if (pagina > totalPaginas)
                pagina = totalPaginas;

            var items = rows
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var vm = new PaginacionVM<UsuarioRowVM>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            ViewBag.Query = q;
            return View(vm);
        }

        public async Task<ActionResult> Create()
        {
            await EnsureRolComercioAsync();

            var allRoles = await ConstruirRolesAsync();

            var vm = new CreateUsuarioVM
            {
                Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.RoleName,
                    Assigned = r.RoleName == "Cliente"
                }).ToList(),
                EsUsuarioComercio = false
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateUsuarioVM vm)
        {
            await EnsureRolComercioAsync();

            vm.Roles = vm.Roles ?? new List<RoleCheckVM>();

            var selectedRoles = vm.Roles
                .Where(r => r.Assigned)
                .Select(r => r.RoleName)
                .ToList();

            if (vm.EsUsuarioComercio && !selectedRoles.Contains("Comercio"))
            {
                selectedRoles.Add("Comercio");
            }

            var esUsuarioComercio = selectedRoles.Contains("Comercio");

            if (esUsuarioComercio)
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
                vm.Password = GenerarContrasenaTemporal();
                vm.ConfirmPassword = vm.Password;
            }

            if (!ModelState.IsValid)
            {
                vm.Roles = await ConstruirRolesAsync(vm.Roles);
                return View(vm);
            }

            var exists = await UserManager.FindByEmailAsync(vm.Email);
            if (exists != null)
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese correo.");
                vm.Roles = await ConstruirRolesAsync(vm.Roles);
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                Nombre = vm.Nombre,
                Apellido = vm.Apellido,
                PhoneNumber = vm.PhoneNumber,
                DebeCambiarContrasena = esUsuarioComercio,
                ContrasenaTemporalActiva = esUsuarioComercio
            };

            var result = await UserManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err);

                vm.Roles = await ConstruirRolesAsync(vm.Roles);
                return View(vm);
            }

            if (selectedRoles.Count == 0)
                selectedRoles.Add("Cliente");

            await UserManager.AddToRolesAsync(user.Id, selectedRoles.ToArray());

            await UserManager.SetLockoutEnabledAsync(user.Id, true);
            if (vm.Bloqueado)
                await UserManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.UtcNow.AddYears(100));

            if (esUsuarioComercio)
            {
                TempData["Ok"] = "Usuario comercio creado correctamente.";
                TempData["TempPasswordTitle"] = "Contraseña temporal generada";
                TempData["TempPasswordText"] = "La contraseña temporal del usuario " + user.Email + " es:";
                TempData["TempPasswordValue"] = vm.Password;
            }
            else
            {
                TempData["Ok"] = "Usuario creado correctamente.";
            }

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction("Index");

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            var userRoles = await UserManager.GetRolesAsync(user.Id);
            var allRoles = await ConstruirRolesAsync();

            var vm = new EditUsuarioVM
            {
                UserId = user.Id,
                Email = user.Email,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                PhoneNumber = user.PhoneNumber,
                Bloqueado = user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc > DateTime.UtcNow,
                DebeCambiarContrasena = user.DebeCambiarContrasena,
                ContrasenaTemporalActiva = user.ContrasenaTemporalActiva,
                Roles = allRoles.Select(r => new RoleCheckVM
                {
                    RoleName = r.RoleName,
                    Assigned = userRoles.Contains(r.RoleName)
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditUsuarioVM vm)
        {
            var user = await UserManager.FindByIdAsync(vm.UserId);
            if (user == null)
                return HttpNotFound();

            user.Nombre = vm.Nombre;
            user.Apellido = vm.Apellido;
            user.PhoneNumber = vm.PhoneNumber;

            var selectedRoles = (vm.Roles ?? new List<RoleCheckVM>())
                .Where(r => r.Assigned)
                .Select(r => r.RoleName)
                .ToList();

            var esUsuarioComercio = selectedRoles.Contains("Comercio");
            user.DebeCambiarContrasena = vm.DebeCambiarContrasena;
            user.ContrasenaTemporalActiva = esUsuarioComercio && vm.ContrasenaTemporalActiva;

            await UserManager.UpdateAsync(user);

            var currentRoles = await UserManager.GetRolesAsync(user.Id);
            var removeRoles = currentRoles.Except(selectedRoles).ToArray();
            var addRoles = selectedRoles.Except(currentRoles).ToArray();

            if (removeRoles.Any())
                await UserManager.RemoveFromRolesAsync(user.Id, removeRoles);

            if (addRoles.Any())
                await UserManager.AddToRolesAsync(user.Id, addRoles);

            await UserManager.SetLockoutEnabledAsync(user.Id, true);

            if (vm.Bloqueado)
                await UserManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.UtcNow.AddYears(100));
            else
                await UserManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.UtcNow);

            TempData["Ok"] = "Usuario actualizado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            using (var db = new ApplicationDbContext())
            {
                var comercioAsignado = db.Comercios.FirstOrDefault(c => c.UserId == id);
                if (comercioAsignado != null)
                {
                    TempData["Err"] = "No se puede eliminar el usuario porque está asignado a un comercio.";
                    return RedirectToAction("Index");
                }
            }

            var result = await UserManager.DeleteAsync(user);
            TempData[result.Succeeded ? "Ok" : "Err"] = result.Succeeded
                ? "Usuario eliminado correctamente."
                : string.Join(" | ", result.Errors);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Err"] = "La nueva contraseña es requerida.";
                return RedirectToAction("Edit", new { id });
            }

            var hasPassword = await UserManager.HasPasswordAsync(user.Id);
            IdentityResult result;

            if (hasPassword)
            {
                var removeResult = await UserManager.RemovePasswordAsync(user.Id);
                if (!removeResult.Succeeded)
                {
                    TempData["Err"] = string.Join(" | ", removeResult.Errors);
                    return RedirectToAction("Edit", new { id });
                }
            }

            result = await UserManager.AddPasswordAsync(user.Id, newPassword);

            if (result.Succeeded)
            {
                user.DebeCambiarContrasena = false;
                user.ContrasenaTemporalActiva = false;
                await UserManager.UpdateAsync(user);
            }

            TempData[result.Succeeded ? "Ok" : "Err"] = result.Succeeded
                ? "Contraseña restablecida correctamente."
                : string.Join(" | ", result.Errors);

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPasswordTemporal(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            var tempPassword = GenerarContrasenaTemporal();

            var hasPassword = await UserManager.HasPasswordAsync(user.Id);
            IdentityResult result;

            if (hasPassword)
            {
                var removeResult = await UserManager.RemovePasswordAsync(user.Id);
                if (!removeResult.Succeeded)
                {
                    TempData["Err"] = string.Join(" | ", removeResult.Errors);
                    return RedirectToAction("Edit", new { id });
                }
            }

            result = await UserManager.AddPasswordAsync(user.Id, tempPassword);

            if (result.Succeeded)
            {
                user.DebeCambiarContrasena = true;
                user.ContrasenaTemporalActiva = true;
                await UserManager.UpdateAsync(user);

                TempData["Ok"] = "Se generó una nueva contraseña temporal.";
                TempData["TempPasswordTitle"] = "Nueva contraseña temporal";
                TempData["TempPasswordText"] = "La contraseña temporal del usuario " + user.Email + " es:";
                TempData["TempPasswordValue"] = tempPassword;
            }
            else
            {
                TempData["Err"] = string.Join(" | ", result.Errors);
            }

            return RedirectToAction("Edit", new { id });
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
        public bool Bloqueado { get; set; }
        public bool DebeCambiarContrasena { get; set; }
        public bool ContrasenaTemporalActiva { get; set; }
    }

    public class EditUsuarioVM
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string PhoneNumber { get; set; }
        public bool Bloqueado { get; set; }
        public bool DebeCambiarContrasena { get; set; }
        public bool ContrasenaTemporalActiva { get; set; }
        public List<RoleCheckVM> Roles { get; set; } = new List<RoleCheckVM>();
    }

    public class CreateUsuarioVM
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        public bool Bloqueado { get; set; }
        public bool EsUsuarioComercio { get; set; }
        public List<RoleCheckVM> Roles { get; set; } = new List<RoleCheckVM>();
    }

    public class RoleCheckVM
    {
        public string RoleName { get; set; }
        public bool Assigned { get; set; }
    }
}