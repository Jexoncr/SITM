using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;
            var userName = "Invitado";

            if (User.Identity.IsAuthenticated)
            {
                var userManager = HttpContext.GetOwinContext()
                    .GetUserManager<UserManager<ApplicationUser>>();

                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var nombreCompleto = ((user.Nombre ?? "") + " " + (user.Apellido ?? "")).Trim();
                    userName = string.IsNullOrWhiteSpace(nombreCompleto) ? user.Email : nombreCompleto;
                }
            }

            var kpiReservasActivas = 0;
            var kpiResenas = 0;
            var kpiEventosProximos = await db.Eventos.CountAsync(e => e.FechaInicio.HasValue && e.FechaInicio >= DateTime.Now);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                kpiReservasActivas = await db.Reservas.CountAsync(r =>
                    r.UserId == userId &&
                    (r.Estado == "Pendiente" || r.Estado == "Confirmada"));

                kpiResenas = await db.Resenas.CountAsync(r => r.UserId == userId);
            }

            var proximosEventos = await db.Eventos
                .Include(e => e.Lugar)
                .Where(e => e.FechaInicio.HasValue && e.FechaInicio >= DateTime.Now)
                .OrderBy(e => e.FechaInicio)
                .Take(3)
                .Select(e => new HomeEventoVM
                {
                    Id = e.Id,
                    Titulo = e.Nombre,
                    Lugar = e.Lugar != null ? e.Lugar.Nombre : "Lugar pendiente",
                    Fecha = e.FechaInicio.Value,
                    Estado = e.CupoMaximo > 0 ? "Programado" : "Cupo limitado",
                    Imagen = null
                })
                .ToListAsync();

            var reservasRecientes = new List<HomeReservaVM>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                reservasRecientes = await db.Reservas
                    .Include(r => r.Evento)
                    .Include(r => r.Lugar)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.FechaReserva)
                    .Take(3)
                    .Select(r => new HomeReservaVM
                    {
                        Id = r.Id,
                        Titulo = r.Evento != null ? r.Evento.Nombre : (r.Lugar != null ? r.Lugar.Nombre : "Reserva"),
                        Fecha = r.FechaReserva,
                        Estado = r.Estado,
                        Plan = r.Evento != null ? "Reserva de evento" : "Reserva"
                    })
                    .ToListAsync();
            }

            var resenasRecientes = new List<HomeResenaVM>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                resenasRecientes = await db.Resenas
                    .Include(r => r.Comercio)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.Fecha)
                    .Take(3)
                    .Select(r => new HomeResenaVM
                    {
                        Id = r.Id,
                        Comercio = r.Comercio != null ? r.Comercio.Nombre : "Comercio",
                        Fecha = r.Fecha,
                        Comentario = r.Comentario,
                        Calificacion = r.Calificacion
                    })
                    .ToListAsync();
            }

            var lugaresDestacados = await db.Lugares
                .Include(l => l.ImagenesLugar)
                .Where(l => l.Estado == "Aprobado" || l.Estado == "Activo" || l.Estado == null)
                .OrderBy(l => l.Nombre)
                .Take(6)
                .ToListAsync();

            var model = new HomeIndexVM
            {
                UserName = userName,
                KpiReservasActivas = kpiReservasActivas,
                KpiResenas = kpiResenas,
                KpiEventosProximos = kpiEventosProximos,
                ProximosEventos = proximosEventos,
                ReservasRecientes = reservasRecientes,
                ResenasRecientes = resenasRecientes,
                LugaresDestacados = lugaresDestacados.Select(l => new HomeLugarVM
                {
                    Titulo = l.Nombre,
                    Sub = string.IsNullOrWhiteSpace(l.Direccion) ? "San Carlos" : l.Direccion,
                    Rating = "Información disponible",
                    Img = l.ImagenesLugar.Select(i => i.UrlImagen).FirstOrDefault() ?? "/Content/im/img2.jpg"
                }).ToList()
            };

            return View(model);
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult Eventos()
        {
            return RedirectToAction("Index", "Eventos");
        }

        public ActionResult Comercios()
        {
            return RedirectToAction("Index", "Comercios");
        }

        public ActionResult Resenas()
        {
            return RedirectToAction("Index", "Resenas");
        }

        [Authorize]
        public ActionResult MisReservas()
        {
            return RedirectToAction("Index", "Reservas");
        }

        public async Task<ActionResult> Mapa()
        {
            var comercioLugarIds = await db.Comercios
                .Select(c => c.LugarId)
                .ToListAsync();

            var lugaresEnt = await db.Lugares
                .Include(l => l.Categoria)
                .Include(l => l.ImagenesLugar)
                .Where(l => l.Estado == "Aprobado" || l.Estado == "Activo" || l.Estado == null)
                .OrderBy(l => l.Nombre)
                .ToListAsync();

            var lugares = lugaresEnt.Select(l => new MapaLugarDTO
            {
                Id = l.Id,
                Nombre = l.Nombre,
                Descripcion = l.Descripcion,
                Categoria = l.Categoria != null ? l.Categoria.Nombre : "Sin categoría",
                Latitud = l.Ubicacion != null && l.Ubicacion.Latitude.HasValue
                    ? (decimal?)l.Ubicacion.Latitude.Value
                    : null,
                Longitud = l.Ubicacion != null && l.Ubicacion.Longitude.HasValue
                    ? (decimal?)l.Ubicacion.Longitude.Value
                    : null,
                Direccion = l.Direccion,
                ImagenUrl = l.ImagenesLugar.Select(i => i.UrlImagen).FirstOrDefault(),
                EsComercio = comercioLugarIds.Contains(l.Id)
            }).ToList();

            var categorias = await db.Categorias
                .OrderBy(c => c.Nombre)
                .Select(c => c.Nombre)
                .ToListAsync();

            var model = new MapaTuristicoVM
            {
                Categorias = categorias,
                Lugares = lugares
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Perfil()
        {
            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<UserManager<ApplicationUser>>();

            var userId = User.Identity.GetUserId();
            var user = await userManager.FindByIdAsync(userId);

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Perfil(ApplicationUser model)
        {
            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<UserManager<ApplicationUser>>();

            var userId = User.Identity.GetUserId();
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
                return HttpNotFound();

            user.Nombre = model.Nombre;
            user.Apellido = model.Apellido;
            user.Canton = model.Canton;
            user.PhoneNumber = model.PhoneNumber;
            user.PrefEventos = model.PrefEventos;
            user.PrefEcologico = model.PrefEcologico;
            user.PrefGastronomia = model.PrefGastronomia;
            user.PrefAventura = model.PrefAventura;
            user.TipoNotificacion = model.TipoNotificacion;
            user.Idioma = model.Idioma;

            await userManager.UpdateAsync(user);

            TempData["Ok"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Perfil");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return RedirectToAction("Register", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class MapaTuristicoVM
    {
        public List<string> Categorias { get; set; }
        public List<MapaLugarDTO> Lugares { get; set; }
    }

    public class MapaLugarDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string Direccion { get; set; }
        public string ImagenUrl { get; set; }
        public bool EsComercio { get; set; }
    }
}