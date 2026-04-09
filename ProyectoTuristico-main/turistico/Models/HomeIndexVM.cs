using System;
using System.Collections.Generic;

namespace turistico.Models
{
    public class HomeIndexVM
    {
        public string UserName { get; set; }

        public int KpiReservasActivas { get; set; }
        public int KpiResenas { get; set; }
        public int KpiEventosProximos { get; set; }

        public List<HomeEventoVM> ProximosEventos { get; set; }
        public List<HomeReservaVM> ReservasRecientes { get; set; }
        public List<HomeResenaVM> ResenasRecientes { get; set; }
        public List<HomeLugarVM> LugaresDestacados { get; set; }
    }

    public class HomeEventoVM
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Lugar { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public string Imagen { get; set; }
    }

    public class HomeReservaVM
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public string Plan { get; set; }
    }

    public class HomeResenaVM
    {
        public int Id { get; set; }
        public string Comercio { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; }
        public int Calificacion { get; set; }
    }

    public class HomeLugarVM
    {
        public string Titulo { get; set; }
        public string Sub { get; set; }
        public string Rating { get; set; }
        public string Img { get; set; }
    }
}