using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class ImagenLugar
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LugarId { get; set; }

        [Required]
        public string UrlImagen { get; set; }

        [ForeignKey("LugarId")]
        public virtual Lugar Lugar { get; set; }
    }
}