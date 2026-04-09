namespace turistico.Models
{
    public class AdminDashboardVM
    {
        public int ComerciosRegistrados { get; set; }
        public int ComerciosPendientes { get; set; }
        public int ComerciosAprobados { get; set; }
        public int ComerciosRechazados { get; set; }

        public int EventosActivos { get; set; }
        public int UsuariosRegistrados { get; set; }
        public int ResenasPendientes { get; set; }
        public int ReservasRegistradas { get; set; }
    }
}