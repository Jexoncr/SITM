using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace turistico.Controllers
{
    public class EventoAdminVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del evento es obligatorio.")]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El comercio es obligatorio.")]
        public int? ComercioId { get; set; }

        public int? LugarId { get; set; }

        [Required(ErrorMessage = "La categoría del evento es obligatoria.")]
        public int CategoriaEventoId { get; set; }

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El cupo máximo no puede ser negativo.")]
        public int CupoMaximo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El límite por persona debe ser mayor que cero.")]
        public int LimitePorPersona { get; set; }

        public HttpPostedFileBase ImagenArchivo { get; set; }
        public string ImagenUrlActual { get; set; }
    }
}