using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{

    public class ComerciosController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: Comercios
        public ActionResult Index()
        {
            List<ComercioDTO> comercios = ObtenerComercios();
            return View(comercios);
        }

        // GET: Comercios/Perfil/5
        public ActionResult Perfil(int id)
        {
            ComercioDTO comercio = ObtenerComercioPorId(id);
            if (comercio == null) return HttpNotFound();
            return View(comercio);
        }

        // GET: Comercios/Contacto/5
        public ActionResult Contacto(int id)
        {
            ComercioDTO comercio = ObtenerComercioPorId(id);
            if (comercio == null) return HttpNotFound();
            return View(comercio);
        }

        private List<ComercioDTO> ObtenerComercios()
        {
            List<ComercioDTO> comercios = new List<ComercioDTO>();

            // QUERY CORREGIDO: Une Lugares con Comercios
            string query = @"
                SELECT 
                    L.Id,
                    COM.Nombre,        
                    COM.Descripcion,   
                    CAT.Nombre AS Categoria,
                    L.Direccion,
                    L.Telefono,
                    L.Horario,
                    L.SitioWeb,
                    (SELECT TOP 1 UrlImagen FROM ImagenesLugar WHERE LugarId = L.Id) AS ImagenUrl
                FROM Lugares L
                INNER JOIN Categorias CAT ON L.CategoriaId = CAT.Id
                INNER JOIN Comercios COM ON L.Id = COM.LugarId
                WHERE L.Estado = 'Aprobado'
                ORDER BY COM.Nombre";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                comercios.Add(MapearComercio(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error SQL: " + ex.Message);
            }

            return comercios;
        }

        private ComercioDTO ObtenerComercioPorId(int id)
        {
            ComercioDTO comercio = null;
            string query = @"
                SELECT L.Id, COM.Nombre, COM.Descripcion, CAT.Nombre AS Categoria,
                       L.Direccion, L.Telefono, L.Horario, L.SitioWeb,
                       (SELECT TOP 1 UrlImagen FROM ImagenesLugar WHERE LugarId = L.Id) AS ImagenUrl
                FROM Lugares L
                INNER JOIN Categorias CAT ON L.CategoriaId = CAT.Id
                INNER JOIN Comercios COM ON L.Id = COM.LugarId
                WHERE L.Id = @Id";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read()) comercio = MapearComercio(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error SQL: " + ex.Message);
            }
            return comercio;
        }

        // Método auxiliar para no repetir código de mapeo
        private ComercioDTO MapearComercio(SqlDataReader reader)
        {
            return new ComercioDTO
            {
                Id = Convert.ToInt32(reader["Id"]),
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"]?.ToString() ?? "",
                Categoria = reader["Categoria"].ToString(),
                Direccion = reader["Direccion"]?.ToString() ?? "",
                Ubicacion = reader["Direccion"]?.ToString() ?? "No especificada",
                Telefono = reader["Telefono"]?.ToString() ?? "",
                Horario = reader["Horario"]?.ToString() ?? "",
                SitioWeb = reader["SitioWeb"]?.ToString() ?? "",
                // Quitamos el ~ para evitar errores de renderizado en el navegador
                ImagenUrl = reader["ImagenUrl"] == DBNull.Value
                            ? "/Content/img/comercios/default.jpg"
                            : reader["ImagenUrl"].ToString().Replace("~", "")
            };
        }
    }
}