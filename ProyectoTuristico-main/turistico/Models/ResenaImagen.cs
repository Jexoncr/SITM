using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class ResenaImagen
    {
        public int Id { get; set; }

        [Required]
        public int ResenaId { get; set; }

        [Required]
        [StringLength(300)]
        public string UrlImagen { get; set; }

        [ForeignKey(nameof(ResenaId))]
        public virtual Resena Resena { get; set; }
    }
}