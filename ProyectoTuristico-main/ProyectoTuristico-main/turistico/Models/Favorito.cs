using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Favorito
    {
        [Key, Column(Order = 0)]
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [Key, Column(Order = 1)]
        public int LugarId { get; set; }
        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        public DateTime FechaGuardado { get; set; } = DateTime.Now;
    }
}
