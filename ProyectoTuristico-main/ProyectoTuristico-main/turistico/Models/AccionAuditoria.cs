using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class AccionAuditoria
    {
        public int Id { get; set; }

        [Required]
        public string AdminUserId { get; set; }
        [ForeignKey(nameof(AdminUserId))]
        public virtual ApplicationUser AdminUser { get; set; }

        [Required, StringLength(100)]
        public string EntidadAfectada { get; set; }

        public int IdEntidad { get; set; }

        [StringLength(100)]
        public string TipoAccion { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
