using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace turistico.Models
{
    public class Lugar
    {
        public int Id { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public virtual Categoria Categoria { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        [StringLength(250)]
        public string Direccion { get; set; }

        [StringLength(400)]
        public string DireccionMapa { get; set; }

        [StringLength(50)]
        public string Telefono { get; set; }

        [StringLength(200)]
        public string Horario { get; set; }

        [StringLength(200)]
        public string SitioWeb { get; set; }

        [StringLength(50)]
        public string Estado { get; set; }

        public DbGeography Ubicacion { get; set; }

        public virtual ICollection<ImagenLugar> ImagenesLugar { get; set; }
        public virtual ICollection<Comercio> Comercios { get; set; }
        public virtual ICollection<Evento> Eventos { get; set; }
        public virtual ICollection<Resena> Resenas { get; set; }
        public virtual ICollection<Reserva> Reservas { get; set; }
        public virtual ICollection<Favorito> Favoritos { get; set; }

        public Lugar()
        {
            ImagenesLugar = new HashSet<ImagenLugar>();
            Comercios = new HashSet<Comercio>();
            Eventos = new HashSet<Evento>();
            Resenas = new HashSet<Resena>();
            Reservas = new HashSet<Reserva>();
            Favoritos = new HashSet<Favorito>();
        }
    }
}