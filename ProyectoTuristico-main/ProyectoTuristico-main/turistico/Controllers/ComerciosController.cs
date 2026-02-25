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

            System.Diagnostics.Debug.WriteLine("==============================");
            System.Diagnostics.Debug.WriteLine("TOTAL: " + comercios.Count);
            foreach (var c in comercios)
            {
                System.Diagnostics.Debug.WriteLine($"  >> {c.Nombre} | WA: [{c.LinkWhatsApp}]");
            }
            System.Diagnostics.Debug.WriteLine("==============================");

            return View(comercios);
        }

        // GET: Comercios/Perfil/5
        // Cambiado a int? para evitar crasheos por parámetros nulos
        public ActionResult Perfil(int? id)
        {
            if (id == null) return HttpNotFound();

            ComercioDTO comercio = ObtenerComercioPorId(id.Value);
            if (comercio == null) return HttpNotFound();

            return View(comercio);
        }

        // GET: Comercios/Contacto/5
        public ActionResult Contacto(int? id)
        {
            if (id == null) return HttpNotFound();

            ComercioDTO comercio = ObtenerComercioPorId(id.Value);
            if (comercio == null) return HttpNotFound();

            return View(comercio);
        }

        private List<ComercioDTO> ObtenerComercios()
        {
            List<ComercioDTO> comercios = new List<ComercioDTO>();

            string query = @"
    SELECT 
        COM.Id,
        COM.Nombre,        
        COM.Descripcion,   
        COM.LinkWhatsApp,
        COM.Telefono,
        ISNULL(CAT.Nombre, 'General') AS Categoria,
        ISNULL(L.Direccion, '')       AS Direccion,
        ISNULL(L.Telefono, '')        AS Telefono,
        ISNULL(L.Horario, '')         AS Horario,
        ISNULL(L.SitioWeb, '')        AS SitioWeb,
        (SELECT TOP 1 UrlImagen 
         FROM ImagenesLugar 
         WHERE LugarId = COM.LugarId) AS ImagenUrl
    FROM Comercios COM
    LEFT JOIN Lugares L   ON COM.LugarId = L.Id
    LEFT JOIN Categorias CAT ON L.CategoriaId = CAT.Id
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
                System.Diagnostics.Debug.WriteLine("Error SQL Index: " + ex.Message);
            }

            System.Diagnostics.Debug.WriteLine("TOTAL COMERCIOS: " + comercios.Count);
            if (comercios.Any())
            {
                System.Diagnostics.Debug.WriteLine("LINK DEL PRIMERO: " + comercios.First().LinkWhatsApp);
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
        COM.Telefono,
        ISNULL(CAT.Nombre, 'General') AS Categoria,
        ISNULL(L.Direccion, '')       AS Direccion,
        ISNULL(L.Telefono, '')        AS Telefono,
        ISNULL(L.Horario, '')         AS Horario,
        ISNULL(L.SitioWeb, '')        AS SitioWeb,
        (SELECT TOP 1 UrlImagen 
         FROM ImagenesLugar 
         WHERE LugarId = COM.LugarId) AS ImagenUrl
    FROM Comercios COM
    LEFT JOIN Lugares L   ON COM.LugarId = L.Id
    LEFT JOIN Categorias CAT ON L.CategoriaId = CAT.Id
    WHERE COM.Id = @Id";
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
                System.Diagnostics.Debug.WriteLine("Error SQL Detalle: " + ex.Message);
            }
            return comercio;
        }

        private ComercioDTO MapearComercio(SqlDataReader reader)
        {
            return new ComercioDTO
            {
                Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                Nombre = reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"]?.ToString() ?? "",

                LinkWhatsApp = reader["LinkWhatsApp"] == DBNull.Value
               ? ""
               : reader["LinkWhatsApp"].ToString(),

                Categoria = reader["Categoria"].ToString(),
                Direccion = reader["Direccion"]?.ToString() ?? "",
                Ubicacion = reader["Direccion"]?.ToString() ?? "No especificada",
                Telefono = reader["Telefono"]?.ToString() ?? "",
                Horario = reader["Horario"]?.ToString() ?? "",
                SitioWeb = reader["SitioWeb"]?.ToString() ?? "",
                ImagenUrl = reader["ImagenUrl"] == DBNull.Value
                            ? "/Content/img/comercios/default.jpg"
                            : reader["ImagenUrl"].ToString().Replace("~", "")
            };
        }
    }
}