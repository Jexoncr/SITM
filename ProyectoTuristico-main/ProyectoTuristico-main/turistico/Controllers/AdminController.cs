using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace turistico.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public ActionResult Dashboard() => View();
        public ActionResult Comercios() => View();
        public ActionResult Eventos() => View();
        public ActionResult Resenas() => View();
        public ActionResult Reservas() => View();
        public ActionResult Usuarios() => View();
    }


}