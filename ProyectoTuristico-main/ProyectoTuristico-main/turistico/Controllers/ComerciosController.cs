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
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var comercios = ObtenerComercios();
            return View(comercios);
        }

        // GET: Comercios/Perfil/5
        public ActionResult Perfil(int? id)
        {
            if (id == null) return HttpNotFound();

            var comercio = ObtenerComercioPorId(id.Value);
            if (comercio == null) return HttpNotFound();

            return View(comercio);
        }

        // GET: Comercios/Contacto/5
        public ActionResult Contacto(int? id)
        {
            if (id == null) return HttpNotFound();

            var comercio = ObtenerComercioPorId(id.Value);
            if (comercio == null) return HttpNotFound();

            return View(comercio);
        }

        private List<ComercioDTO> ObtenerComercios()
        {
            var comercios = new List<ComercioDTO>();

            string query = @"
                SELECT 
                    COM.Id,
                    COM.Nombre,
                    COM.Descripcion,
                    COM.LinkWhatsApp,
                    COALESCE(COM.Telefono, L.Telefono, '') AS Telefono,
                    ISNULL(CAT.Nombre, 'General') AS Categoria,
                    ISNULL(L.Direccion, '')       AS Direccion,
                    ISNULL(L.Horario, '')         AS Horario,
                    ISNULL(L.SitioWeb, '')        AS SitioWeb,
                    (SELECT TOP 1 UrlImagen 
                     FROM ImagenesLugar 
                     WHERE LugarId = COM.LugarId) AS ImagenUrl
                FROM Comercios COM
                LEFT JOIN Lugares L        ON COM.LugarId = L.Id
                LEFT JOIN Categorias CAT   ON L.CategoriaId = CAT.Id
                ORDER BY COM.Nombre;";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comercios.Add(MapearComercio(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error SQL Index: " + ex.Message);
            }

            return comercios;
        }

        private ComercioDTO ObtenerComercioPorId(int id)
        {
            ComercioDTO comercio = null;

            string query = @"
                SELECT 
                    COM.Id,
                    COM.Nombre,
                    COM.Descripcion,
                    COM.LinkWhatsApp,
                    COALESCE(COM.Telefono, L.Telefono, '') AS Telefono,
                    ISNULL(CAT.Nombre, 'General') AS Categoria,
                    ISNULL(L.Direccion, '')       AS Direccion,
                    ISNULL(L.Horario, '')         AS Horario,
                    ISNULL(L.SitioWeb, '')        AS SitioWeb,
                    (SELECT TOP 1 UrlImagen 
                     FROM ImagenesLugar 
                     WHERE LugarId = COM.LugarId) AS ImagenUrl
                FROM Comercios COM
                LEFT JOIN Lugares L        ON COM.LugarId = L.Id
                LEFT JOIN Categorias CAT   ON L.CategoriaId = CAT.Id
                WHERE COM.Id = @Id;";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                                comercio = MapearComercio(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error SQL Detalle: " + ex.Message);
            }

            return comercio;
        }

        private ComercioDTO MapearComercio(SqlDataReader reader)
        {
            string urlDb = reader["ImagenUrl"] == DBNull.Value ? "" : reader["ImagenUrl"].ToString();

            return new ComercioDTO
            {
                Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                Nombre = reader["Nombre"]?.ToString() ?? "",
                Descripcion = reader["Descripcion"]?.ToString() ?? "",
                LinkWhatsApp = reader["LinkWhatsApp"] == DBNull.Value ? "" : reader["LinkWhatsApp"].ToString(),

                Categoria = reader["Categoria"]?.ToString() ?? "General",
                Ubicacion = reader["Direccion"]?.ToString() ?? "No especificada",
                Telefono = reader["Telefono"]?.ToString() ?? "",
                Horario = reader["Horario"]?.ToString() ?? "",
                SitioWeb = reader["SitioWeb"]?.ToString() ?? "",

                // Ajusta el default a lo que exista en tu proyecto
                ImagenUrl = string.IsNullOrWhiteSpace(urlDb)
                    ? "/Content/img/comercios/default.jpg"
                    : urlDb.Replace("~", "").Trim()
            };
        }
    }
}