using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using turistico.Models;

namespace turistico.Controllers
{
    public class ComerciosController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult Index()
        {
            return View(ObtenerComercios());
        }

        public ActionResult Perfil(int id)
        {
            ComercioDTO comercio = ObtenerComercioPorId(id);
            if (comercio == null) return HttpNotFound();
            return View(comercio);
        }

        public ActionResult Contacto(int id)
        {
            ComercioDTO comercio = ObtenerComercioPorId(id);
            if (comercio == null) return HttpNotFound();
            return View(comercio);
        }

        private List<ComercioDTO> ObtenerComercios()
        {
            List<ComercioDTO> comercios = new List<ComercioDTO>();
            string query = @"
                SELECT L.Id, COM.Nombre, COM.Descripcion, CAT.Nombre AS Categoria,
                       L.Direccion, L.Telefono, L.Horario, L.SitioWeb,
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
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comercios.Add(MapearComercio(reader));
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
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
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            return comercio;
        }

        private ComercioDTO MapearComercio(SqlDataReader reader)
        {
            string urlDb = reader["ImagenUrl"]?.ToString();
            return new ComercioDTO
            {
                Id = Convert.ToInt32(reader["Id"]),
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"]?.ToString() ?? "",
                Categoria = reader["Categoria"].ToString(),
                Ubicacion = reader["Direccion"]?.ToString() ?? "No especificada",
                Telefono = reader["Telefono"]?.ToString() ?? "",
                Horario = reader["Horario"]?.ToString() ?? "",
                SitioWeb = reader["SitioWeb"]?.ToString() ?? "",
                // AJUSTE FINAL: La ruta debe empezar con /Content/img/
                ImagenUrl = string.IsNullOrWhiteSpace(urlDb)
                            ? "/Content/img/default.jpg"
                            : urlDb.Trim()
            };
        }
    }
}