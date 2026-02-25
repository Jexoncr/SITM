using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Comercio
    {
        public int Id { get; set; }

        [Required]
        public int LugarId { get; set; }
        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }
        public string LinkWhatsApp { get; set; }


        public virtual ICollection<ProductoServicio> ProductosServicios { get; set; }
        public virtual ComercioRegulado ComercioRegulado { get; set; } // 1 a 1 (opcional)
    }
}
