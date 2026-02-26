using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace turistico.Models
{
    public class AdminDashboardVM
    {
        public int ComerciosRegistrados { get; set; }
        public int ComerciosPendientes { get; set; }
        public int EventosActivos { get; set; }
        public int UsuariosRegistrados { get; set; }

        public int[] EventosPorMes { get; set; } = new int[12];
        public int ComerciosAprobados { get; set; }
        public int ComerciosRechazados { get; set; }
    }
}