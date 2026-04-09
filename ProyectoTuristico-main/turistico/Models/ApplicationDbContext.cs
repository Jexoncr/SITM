using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Spatial;

namespace turistico.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellido { get; set; }

        [StringLength(150)]
        public string Canton { get; set; }

        public bool PrefEventos { get; set; }
        public bool PrefEcologico { get; set; }
        public bool PrefGastronomia { get; set; }
        public bool PrefAventura { get; set; }

        [StringLength(50)]
        public string TipoNotificacion { get; set; }

        [StringLength(50)]
        public string Idioma { get; set; }

        public bool DebeCambiarContrasena { get; set; }
        public bool ContrasenaTemporalActiva { get; set; }

        public virtual ICollection<Resena> Resenas { get; set; }
        public virtual ICollection<Reserva> Reservas { get; set; }
        public virtual ICollection<Favorito> Favoritos { get; set; }
        public virtual ICollection<AccionAuditoria> AccionesAuditoria { get; set; }

        public ApplicationUser()
        {
            Resenas = new HashSet<Resena>();
            Reservas = new HashSet<Reserva>();
            Favoritos = new HashSet<Favorito>();
            AccionesAuditoria = new HashSet<AccionAuditoria>();
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<CategoriaEvento> CategoriasEvento { get; set; }

        public DbSet<Lugar> Lugares { get; set; }
        public DbSet<ImagenLugar> ImagenesLugar { get; set; }

        public DbSet<Comercio> Comercios { get; set; }
        public DbSet<ComercioRegulado> ComerciosRegulados { get; set; }
        public DbSet<ProductoServicio> ProductosServicios { get; set; }

        public DbSet<Evento> Eventos { get; set; }

        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        public DbSet<Resena> Resenas { get; set; }
        public DbSet<ResenaImagen> ResenaImagenes { get; set; }

        public DbSet<AccionAuditoria> AccionesAuditoria { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Categoria>().ToTable("Categorias");
            modelBuilder.Entity<CategoriaEvento>().ToTable("CategoriasEvento");
            modelBuilder.Entity<Lugar>().ToTable("Lugares");
            modelBuilder.Entity<ImagenLugar>().ToTable("ImagenesLugar");
            modelBuilder.Entity<Comercio>().ToTable("Comercios");
            modelBuilder.Entity<ComercioRegulado>().ToTable("ComerciosRegulados");
            modelBuilder.Entity<ProductoServicio>().ToTable("ProductosServicio");
            modelBuilder.Entity<Evento>().ToTable("Eventos");
            modelBuilder.Entity<Favorito>().ToTable("Favoritos");
            modelBuilder.Entity<Reserva>().ToTable("Reservas");
            modelBuilder.Entity<Resena>().ToTable("Resenas");
            modelBuilder.Entity<ResenaImagen>().ToTable("ResenaImagenes");
            modelBuilder.Entity<AccionAuditoria>().ToTable("AccionesAuditoria");

            modelBuilder.Entity<ProductoServicio>()
                .Property(x => x.Precio)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Favorito>()
                .HasKey(f => new { f.UserId, f.LugarId });

            modelBuilder.Entity<Comercio>()
                .HasRequired(c => c.Lugar)
                .WithMany(l => l.Comercios)
                .HasForeignKey(c => c.LugarId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Comercio>()
                .HasOptional(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ComercioRegulado>()
                .HasRequired(cr => cr.Comercio)
                .WithOptional(c => c.ComercioRegulado);

            modelBuilder.Entity<Evento>()
                .HasRequired(e => e.Lugar)
                .WithMany(l => l.Eventos)
                .HasForeignKey(e => e.LugarId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Evento>()
                .HasOptional(e => e.Comercio)
                .WithMany(c => c.Eventos)
                .HasForeignKey(e => e.ComercioId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Evento>()
                .HasRequired(e => e.CategoriaEvento)
                .WithMany(c => c.Eventos)
                .HasForeignKey(e => e.CategoriaEventoId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Reserva>()
                .HasRequired(r => r.User)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Reserva>()
                .HasRequired(r => r.Lugar)
                .WithMany(l => l.Reservas)
                .HasForeignKey(r => r.LugarId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Reserva>()
                .HasOptional(r => r.Evento)
                .WithMany(e => e.Reservas)
                .HasForeignKey(r => r.EventoId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Resena>()
                .HasRequired(r => r.User)
                .WithMany(u => u.Resenas)
                .HasForeignKey(r => r.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Resena>()
                .HasRequired(r => r.Lugar)
                .WithMany(l => l.Resenas)
                .HasForeignKey(r => r.LugarId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Resena>()
                .HasOptional(r => r.Comercio)
                .WithMany()
                .HasForeignKey(r => r.ComercioId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Resena>()
                .HasOptional(r => r.Evento)
                .WithMany()
                .HasForeignKey(r => r.EventoId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Resena>()
                .HasOptional(r => r.ModeradoPor)
                .WithMany()
                .HasForeignKey(r => r.ModeradoPorUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ResenaImagen>()
                .HasRequired(ri => ri.Resena)
                .WithMany(r => r.Imagenes)
                .HasForeignKey(ri => ri.ResenaId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<AccionAuditoria>()
                .HasRequired(a => a.AdminUser)
                .WithMany(u => u.AccionesAuditoria)
                .HasForeignKey(a => a.AdminUserId)
                .WillCascadeOnDelete(false);
        }
    }
}