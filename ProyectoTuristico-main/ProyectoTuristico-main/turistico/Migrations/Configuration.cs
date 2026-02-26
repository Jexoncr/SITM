using System;
using System.Data.Entity.Migrations;
using System.Linq;
using turistico.Models;

namespace turistico.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<turistico.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(turistico.Models.ApplicationDbContext context)
        {
            // =========================
            // 1) Categorías
            // =========================
            context.Categorias.AddOrUpdate(c => c.Nombre,
                new Categoria { Nombre = "Turismo", Descripcion = "Lugares turísticos y atractivos naturales." },
                new Categoria { Nombre = "Cultura", Descripcion = "Museos, patrimonio, historia y cultura local." },
                new Categoria { Nombre = "Gastronomía", Descripcion = "Restaurantes, sodas y experiencias culinarias." },
                new Categoria { Nombre = "Aventura", Descripcion = "Actividades al aire libre, tours y deportes." }
            );
            context.SaveChanges();

            // =========================
            // 2) Categorías de Evento
            // =========================
            context.CategoriasEvento.AddOrUpdate(ce => ce.Nombre,
                new CategoriaEvento { Nombre = "Festival", Descripcion = "Festivales culturales y gastronómicos." },
                new CategoriaEvento { Nombre = "Concierto", Descripcion = "Eventos musicales y shows." },
                new CategoriaEvento { Nombre = "Feria", Descripcion = "Ferias artesanales y comerciales." },
                new CategoriaEvento { Nombre = "Deportivo", Descripcion = "Eventos y actividades deportivas." }
            );
            context.SaveChanges();

            // Helpers para obtener IDs
            var catTurismo = context.Categorias.First(c => c.Nombre == "Turismo");
            var catCultura = context.Categorias.First(c => c.Nombre == "Cultura");
            var catGastro = context.Categorias.First(c => c.Nombre == "Gastronomía");
            var catAventura = context.Categorias.First(c => c.Nombre == "Aventura");

            var evFestival = context.CategoriasEvento.First(c => c.Nombre == "Festival");
            var evFeria = context.CategoriasEvento.First(c => c.Nombre == "Feria");

            // =========================
            // 3) Lugares
            // =========================
            // (Usamos Nombre como clave natural para AddOrUpdate)
            context.Lugares.AddOrUpdate(l => l.Nombre,
                new Lugar
                {
                    CategoriaId = catTurismo.Id,
                    Nombre = "Parque Central",
                    Descripcion = "Punto de encuentro principal con zonas verdes y áreas para actividades.",
                    Direccion = "Centro",
                    Telefono = "0000-0000",
                    Horario = "6:00 AM - 9:00 PM",
                    SitioWeb = "https://example.com/parque",
                    Estado = "Activo",
                    Latitud = 10.092000m,
                    Longitud = -84.471000m
                },
                new Lugar
                {
                    CategoriaId = catCultura.Id,
                    Nombre = "Museo Municipal",
                    Descripcion = "Exhibiciones de historia local y patrimonio cultural.",
                    Direccion = "Avenida Principal",
                    Telefono = "0000-0001",
                    Horario = "9:00 AM - 5:00 PM",
                    SitioWeb = "https://example.com/museo",
                    Estado = "Activo",
                    Latitud = 10.090500m,
                    Longitud = -84.470200m
                },
                new Lugar
                {
                    CategoriaId = catGastro.Id,
                    Nombre = "Mercado Gastronómico",
                    Descripcion = "Comida típica, productos locales y emprendimientos.",
                    Direccion = "Barrio Centro",
                    Telefono = "0000-0002",
                    Horario = "8:00 AM - 6:00 PM",
                    SitioWeb = "https://example.com/mercado",
                    Estado = "Activo",
                    Latitud = 10.091200m,
                    Longitud = -84.472200m
                },
                new Lugar
                {
                    CategoriaId = catAventura.Id,
                    Nombre = "Mirador Natural",
                    Descripcion = "Sendero con vista panorámica y zona de descanso.",
                    Direccion = "Ruta 123",
                    Telefono = "0000-0003",
                    Horario = "7:00 AM - 4:00 PM",
                    SitioWeb = "https://example.com/mirador",
                    Estado = "Activo",
                    Latitud = 10.100100m,
                    Longitud = -84.480500m
                }
            );
            context.SaveChanges();

            // Traer lugares ya insertados (para usar IDs)
            var parque = context.Lugares.First(l => l.Nombre == "Parque Central");
            var museo = context.Lugares.First(l => l.Nombre == "Museo Municipal");
            var mercado = context.Lugares.First(l => l.Nombre == "Mercado Gastronómico");

            // =========================
            // 4) Imágenes por lugar
            // =========================
            context.ImagenesLugar.AddOrUpdate(i => i.UrlImagen,
                new ImagenLugar { LugarId = parque.Id, UrlImagen = "https://picsum.photos/seed/parque/900/600" },
                new ImagenLugar { LugarId = parque.Id, UrlImagen = "https://picsum.photos/seed/parque2/900/600" },
                new ImagenLugar { LugarId = museo.Id, UrlImagen = "https://picsum.photos/seed/museo/900/600" },
                new ImagenLugar { LugarId = mercado.Id, UrlImagen = "https://picsum.photos/seed/mercado/900/600" }
            );
            context.SaveChanges();

            // =========================
            // 5) Eventos
            // =========================
            context.Eventos.AddOrUpdate(e => e.Nombre,
                new Evento
                {
                    LugarId = parque.Id,
                    CategoriaEventoId = evFestival.Id,
                    Nombre = "Festival Cultural SITM",
                    Descripcion = "Música, arte y gastronomía local.",
                    FechaInicio = DateTime.Today.AddDays(10).AddHours(10),
                    FechaFin = DateTime.Today.AddDays(10).AddHours(18)
                },
                new Evento
                {
                    LugarId = museo.Id,
                    CategoriaEventoId = evFeria.Id,
                    Nombre = "Feria Artesanal",
                    Descripcion = "Artesanías locales, emprendimientos y actividades.",
                    FechaInicio = DateTime.Today.AddDays(20).AddHours(9),
                    FechaFin = DateTime.Today.AddDays(20).AddHours(17)
                }
            );
            context.SaveChanges();

            // =========================
            // 6) Comercios
            // =========================
            context.Comercios.AddOrUpdate(c => c.Nombre,
                new Comercio
                {
                    LugarId = mercado.Id,
                    Nombre = "Soda Tica",
                    Descripcion = "Comida típica costarricense."
                },
                new Comercio
                {
                    LugarId = mercado.Id,
                    Nombre = "Café Local",
                    Descripcion = "Café de la zona y repostería."
                }
            );
            context.SaveChanges();

            // =========================
            // 7) Productos / Servicios
            // =========================
            var soda = context.Comercios.First(c => c.Nombre == "Soda Tica");
            var cafe = context.Comercios.First(c => c.Nombre == "Café Local");

            context.ProductosServicios.AddOrUpdate(p => new { p.ComercioId, p.Nombre },
                new ProductoServicio { ComercioId = soda.Id, Nombre = "Casado", Precio = 3500m },
                new ProductoServicio { ComercioId = soda.Id, Nombre = "Gallo Pinto", Precio = 2500m },
                new ProductoServicio { ComercioId = cafe.Id, Nombre = "Café Negro", Precio = 1200m },
                new ProductoServicio { ComercioId = cafe.Id, Nombre = "Capuccino", Precio = 2000m }
            );
            context.SaveChanges();

            // Nota: Favoritos/Reseñas/Reservas normalmente se crean con usuarios reales logueados,
            // por eso no los seedeo aquí (pero si querés, lo hacemos).
        }
    }
}
