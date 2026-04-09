using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Evento
    {
        public int Id { get; set; }

        [Required]
        public int LugarId { get; set; }

        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        public int? ComercioId { get; set; }

        [ForeignKey(nameof(ComercioId))]
        public virtual Comercio Comercio { get; set; }

        [Required]
        public int CategoriaEventoId { get; set; }

        [ForeignKey(nameof(CategoriaEventoId))]
        public virtual CategoriaEvento CategoriaEvento { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public int CupoMaximo { get; set; }
        public int LimitePorPersona { get; set; }

        [StringLength(500)]
        public string ImagenUrl { get; set; }

        public virtual ICollection<Reserva> Reservas { get; set; }
    }
}