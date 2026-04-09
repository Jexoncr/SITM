using System.Collections.Generic;

namespace turistico.Models
{
    public class MapaTuristicoVM
    {
        public List<string> Categorias { get; set; }
        public List<MapaLugarDTO> Lugares { get; set; }

        public MapaTuristicoVM()
        {
            Categorias = new List<string>();
            Lugares = new List<MapaLugarDTO>();
        }
    }

    public class MapaLugarDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string Direccion { get; set; }
        public string ImagenUrl { get; set; }
        public bool EsComercio { get; set; }
    }
}