using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace turistico.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(300)]
        public string Descripcion { get; set; }

        public virtual ICollection<Lugar> Lugares { get; set; }
    }
}