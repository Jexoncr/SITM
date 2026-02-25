using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Dashboard()
        {
            var vm = new AdminDashboardVM
            {
                ComerciosRegistrados = db.Comercios.Count(),
                ComerciosPendientes = db.Comercios.Count(c => c.Lugar.Estado == "Pendiente"),
                ComerciosAprobados = db.Comercios.Count(c => c.Lugar.Estado == "Aprobado"),
                ComerciosRechazados = db.Comercios.Count(c => c.Lugar.Estado == "Rechazado"),
                UsuariosRegistrados = db.Users.Count()
            };

            return View(vm);
        }
    }
}