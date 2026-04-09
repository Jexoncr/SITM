using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRolesController : Controller
    {
        private RoleManager<IdentityRole> RoleManager
        {
            get
            {
                return HttpContext.GetOwinContext().Get<RoleManager<IdentityRole>>();
            }
        }

        public ActionResult Index()
        {
            var roles = RoleManager.Roles.OrderBy(r => r.Name).ToList();
            return View(roles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(string name)
        {
            name = (name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Err"] = "El nombre del rol es requerido.";
                return RedirectToAction("Index");
            }

            if (await RoleManager.RoleExistsAsync(name))
            {
                TempData["Err"] = "Ese rol ya existe.";
                return RedirectToAction("Index");
            }

            var res = await RoleManager.CreateAsync(new IdentityRole(name));

            TempData[res.Succeeded ? "Ok" : "Err"] = res.Succeeded
                ? "Rol creado correctamente."
                : string.Join(" | ", res.Errors);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id)
        {
            var role = await RoleManager.FindByIdAsync(id);
            if (role == null)
                return HttpNotFound();

            if (role.Name == "Admin")
            {
                TempData["Err"] = "No se puede eliminar el rol Admin.";
                return RedirectToAction("Index");
            }

            var res = await RoleManager.DeleteAsync(role);

            TempData[res.Succeeded ? "Ok" : "Err"] = res.Succeeded
                ? "Rol eliminado correctamente."
                : string.Join(" | ", res.Errors);

            return RedirectToAction("Index");
        }
    }
}