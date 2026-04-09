using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    public class ComerciosController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 9;

        public async Task<ActionResult> Index(int pagina = 1)
        {
            if (pagina < 1)
                pagina = 1;

            var query = db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.Lugar.Categoria)
                .Include(c => c.Lugar.ImagenesLugar)
                .Where(c => c.Lugar.Estado == "Aprobado" || c.Lugar.Estado == "Activo" || c.Lugar.Estado == null)
                .OrderBy(c => c.Nombre)
                .Select(c => new ComercioDTO
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    LinkWhatsApp = c.LinkWhatsApp,
                    Categoria = c.Lugar.Categoria.Nombre,
                    Direccion = c.Lugar.Direccion,
                    Ubicacion = c.Lugar.Direccion,
                    Telefono = c.Telefono ?? c.Lugar.Telefono,
                    Horario = c.Lugar.Horario,
                    SitioWeb = c.Lugar.SitioWeb,
                    ImagenUrl = c.Lugar.ImagenesLugar.Select(i => i.UrlImagen).FirstOrDefault()
                });

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)totalRegistros / PageSize);

            if (totalPaginas == 0)
                totalPaginas = 1;

            if (pagina > totalPaginas)
                pagina = totalPaginas;

            var items = await query
                .Skip((pagina - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ImagenUrl))
                    item.ImagenUrl = "/Content/img/comercios/default.jpg";
            }

            var model = new PaginacionVM<ComercioDTO>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                RegistrosPorPagina = PageSize
            };

            return View(model);
        }

        public async Task<ActionResult> Perfil(int? id)
        {
            if (!id.HasValue)
                return HttpNotFound();

            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .Include(c => c.Lugar.Categoria)
                .Include(c => c.Lugar.ImagenesLugar)
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (comercio == null)
                return HttpNotFound();

            var model = new ComercioDTO
            {
                Id = comercio.Id,
                Nombre = comercio.Nombre,
                Descripcion = comercio.Descripcion,
                Categoria = comercio.Lugar?.Categoria?.Nombre ?? "General",
                Ubicacion = comercio.Lugar?.Direccion,
                Direccion = comercio.Lugar?.Direccion,
                Telefono = comercio.Telefono ?? comercio.Lugar?.Telefono,
                Horario = comercio.Lugar?.Horario,
                SitioWeb = comercio.Lugar?.SitioWeb,
                LinkWhatsApp = comercio.LinkWhatsApp,
                ImagenUrl = comercio.Lugar?.ImagenesLugar?.Select(i => i.UrlImagen).FirstOrDefault()
            };

            if (string.IsNullOrWhiteSpace(model.ImagenUrl))
                model.ImagenUrl = "/Content/img/comercios/default.jpg";

            return View(model);
        }

        public async Task<ActionResult> Contacto(int? id)
        {
            if (!id.HasValue)
                return HttpNotFound();

            var comercio = await db.Comercios
                .Include(c => c.Lugar)
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (comercio == null)
                return HttpNotFound();

            var model = new ComercioDTO
            {
                Id = comercio.Id,
                Nombre = comercio.Nombre,
                Telefono = comercio.Telefono ?? comercio.Lugar?.Telefono,
                SitioWeb = comercio.Lugar?.SitioWeb,
                LinkWhatsApp = comercio.LinkWhatsApp,
                Direccion = comercio.Lugar?.Direccion,
                Ubicacion = comercio.Lugar?.Direccion
            };

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}