using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class ImagenLugar
    {
        public int Id { get; set; }

        [Required]
        public int LugarId { get; set; }

        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        [Required, StringLength(255)]
        public string UrlImagen { get; set; }
    }
}
