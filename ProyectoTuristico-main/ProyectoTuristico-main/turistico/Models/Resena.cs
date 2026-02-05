using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Resena
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int LugarId { get; set; }
        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        [Range(1, 5)]
        public int Calificacion { get; set; }

        [StringLength(500)]
        public string Comentario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
