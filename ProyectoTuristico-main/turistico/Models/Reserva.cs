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

        [Required]
        public int LugarId { get; set; }

        public int? EventoId { get; set; }

        [Range(1, 1000)]
        public int CantidadPersonas { get; set; }

        [StringLength(30)]
        public string Estado { get; set; } 

        public DateTime FechaReserva { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        [ForeignKey(nameof(EventoId))]
        public virtual Evento Evento { get; set; }

        public Reserva()
        {
            FechaReserva = DateTime.Now;
            Estado = "Pendiente"; 
        }
    }
}