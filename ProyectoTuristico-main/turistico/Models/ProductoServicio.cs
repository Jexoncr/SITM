using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class ProductoServicio
    {
        public int Id { get; set; }

        [Required]
        public int ComercioId { get; set; }

        [ForeignKey(nameof(ComercioId))]
        public virtual Comercio Comercio { get; set; }

        [StringLength(150)]
        public string Nombre { get; set; }

        public decimal? Precio { get; set; }
    }
}