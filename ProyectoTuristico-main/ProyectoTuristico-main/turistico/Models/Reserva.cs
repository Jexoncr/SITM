using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Reserva
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

        public DateTime FechaReserva { get; set; }

        [Range(1, 1000)]
        public int NumeroPersonas { get; set; }

        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente";
    }
}
