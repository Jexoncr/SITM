using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Lugar
    {
        public int Id { get; set; }

        [Required]
        public int CategoriaId { get; set; }
        [ForeignKey(nameof(CategoriaId))]
        public virtual Categoria Categoria { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }

        [StringLength(250)]
        public string Direccion { get; set; }

        [StringLength(50)]
        public string Telefono { get; set; }

        [StringLength(200)]
        public string Horario { get; set; }

        [StringLength(200)]
        public string SitioWeb { get; set; }

        [StringLength(50)]
        public string Estado { get; set; } // Activo / Inactivo

       
        public virtual ICollection<ImagenLugar> ImagenesLugar { get; set; }
    }
}
