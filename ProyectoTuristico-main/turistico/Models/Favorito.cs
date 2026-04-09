using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace turistico.Models
{
    public class Favorito
    {
        public string UserId { get; set; }
        public int LugarId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey(nameof(LugarId))]
        public virtual Lugar Lugar { get; set; }

        public DateTime FechaGuardado { get; set; }
    }
}