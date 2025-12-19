using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace turistico.Controllers
{
    public class HomeController : Controller
    {
        

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Eventos()
        {
            return View();
        }

        public ActionResult Comercios()
        {
            return View();
        }
        public ActionResult Resenas()
        {
            return View();
        }
        public ActionResult Perfil()
        {
            return View();
        }
        public ActionResult Mapa()
        {
            return View();
        }
        public ActionResult Recuperar()
        {
            return View();
        }
        public ActionResult MisReservas()
        {
            return View();
        }



    }
}