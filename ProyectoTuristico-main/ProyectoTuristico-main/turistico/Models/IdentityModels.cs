using Microsoft.AspNet.Identity.EntityFramework;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace turistico.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellido { get; set; }

        // Perfil
        [StringLength(150)]
        public string Canton { get; set; }

        // Preferencias turísticas
        public bool PrefEventos { get; set; }
        public bool PrefEcologico { get; set; }
        public bool PrefGastronomia { get; set; }
        public bool PrefAventura { get; set; }

        // Preferencias de sistema
        [StringLength(50)]
        public string TipoNotificacion { get; set; } // "Sistema", "Correo", "SMS", etc.

        [StringLength(50)]
        public string Idioma { get; set; } // "Español", "Inglés", etc.
    }


    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("DefaultConnection") { }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        
        //  MÓDULOS DEL SISTEMA 
        

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Lugar> Lugares { get; set; }
        public DbSet<ImagenLugar> ImagenesLugar { get; set; }

        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<Resena> Resenas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        public DbSet<CategoriaEvento> CategoriasEvento { get; set; }
        public DbSet<Evento> Eventos { get; set; }

        public DbSet<Comercio> Comercios { get; set; }
        public DbSet<ComercioRegulado> ComerciosRegulados { get; set; }
        public DbSet<ProductoServicio> ProductosServicios { get; set; }

        public DbSet<AccionAuditoria> AccionesAuditoria { get; set; }
       

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            modelBuilder.Entity<Lugar>().ToTable("Lugares");
            modelBuilder.Entity<ImagenLugar>().ToTable("ImagenesLugar");

            modelBuilder.Entity<Categoria>().ToTable("Categorias");
            modelBuilder.Entity<CategoriaEvento>().ToTable("CategoriasEvento");
            modelBuilder.Entity<Evento>().ToTable("Eventos");

            modelBuilder.Entity<Favorito>().ToTable("Favoritos");
            modelBuilder.Entity<Resena>().ToTable("Resenas");
            modelBuilder.Entity<Reserva>().ToTable("Reservas");

            modelBuilder.Entity<Comercio>().ToTable("Comercios");
            modelBuilder.Entity<ComercioRegulado>().ToTable("ComerciosRegulados");
            modelBuilder.Entity<ProductoServicio>().ToTable("ProductosServicio");

            modelBuilder.Entity<AccionAuditoria>().ToTable("AccionesAuditoria");
        }
    }
}
