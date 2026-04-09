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

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        [StringLength(255)]
        public string LinkWhatsApp { get; set; }

        [StringLength(20)]
        public string Telefono { get; set; }

        [StringLength(128)]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        public virtual ComercioRegulado ComercioRegulado { get; set; }
        public virtual ICollection<ProductoServicio> ProductosServicios { get; set; }
        public virtual ICollection<Evento> Eventos { get; set; }

        public Comercio()
        {
            ProductosServicios = new HashSet<ProductoServicio>();
            Eventos = new HashSet<Evento>();
        }
    }
}