using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Resena
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int LugarId { get; set; }

        public int? ComercioId { get; set; }
        public int? EventoId { get; set; }

        [Required]
        [StringLength(30)]
        public string Tipo { get; set; }

        [Range(1, 5)]
        public int Calificacion { get; set; }

        [StringLength(1000)]
        public string Comentario { get; set; }

        public DateTime Fecha { get; set; }

        [StringLength(30)]
        public string Estado { get; set; }

        public DateTime? FechaModeracion { get; set; }

        public string ModeradoPorUserId { get; set; }

        [StringLength(500)]
        public string MotivoModeracion { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        [ForeignKey(nameof(ComercioId))]
        public virtual Comercio Comercio { get; set; }

        [ForeignKey(nameof(EventoId))]
        public virtual Evento Evento { get; set; }

        [ForeignKey(nameof(ModeradoPorUserId))]
        public virtual ApplicationUser ModeradoPor { get; set; }

        public virtual ICollection<ResenaImagen> Imagenes { get; set; }

        public Resena()
        {
            Fecha = DateTime.Now;
            Estado = "Pendiente";
            Imagenes = new HashSet<ResenaImagen>();
        }
    }
}