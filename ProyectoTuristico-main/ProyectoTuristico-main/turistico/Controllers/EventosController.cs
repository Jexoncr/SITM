using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TuProyecto.Controllers
{
    public class EventosController : Controller
    {

        public ActionResult Detalle(int id)
        {
            ViewBag.Id = id;
            return View();
        }

        public ActionResult Contacto(int id)
        {
            ViewBag.Id = id;
            return View();
        }
    }
}
