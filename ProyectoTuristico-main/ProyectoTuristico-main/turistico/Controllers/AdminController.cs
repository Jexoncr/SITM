using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace turistico.Controllers
{
    public class AdminController : Controller
    {
        public ActionResult Dashboard()
        {
            return View();
        }

        public ActionResult Comercios()
        {
            return View();
        }

        public ActionResult Eventos()
        {
            return View();
        }

        public ActionResult Resenas()
        {
            return View();
        }

        public ActionResult Reservas()
        {
            return View();
        }

        public ActionResult Usuarios()
        {
            return View();
        }
    }

}