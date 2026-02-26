using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class ComercioRegulado
    {
        [Key, ForeignKey(nameof(Comercio))]
        public int ComercioId { get; set; }
        public virtual Comercio Comercio { get; set; }

        [Required, StringLength(100)]
        public string NumeroPatente { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        [StringLength(50)]
        public string EstadoValidacion { get; set; }
    }
}
