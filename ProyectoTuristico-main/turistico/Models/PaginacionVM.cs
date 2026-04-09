using System.Collections.Generic;

namespace turistico.Models
{
    public class PaginacionVM<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalRegistros { get; set; }
        public int RegistrosPorPagina { get; set; }

        public bool TieneAnterior => PaginaActual > 1;
        public bool TieneSiguiente => PaginaActual < TotalPaginas;

        public int Desde
        {
            get
            {
                if (TotalRegistros == 0) return 0;
                return ((PaginaActual - 1) * RegistrosPorPagina) + 1;
            }
        }

        public int Hasta
        {
            get
            {
                var hasta = PaginaActual * RegistrosPorPagina;
                return hasta > TotalRegistros ? TotalRegistros : hasta;
            }
        }
    }
}