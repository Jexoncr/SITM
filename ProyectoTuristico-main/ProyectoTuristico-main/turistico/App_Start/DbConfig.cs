using System.Data.Entity;
using turistico.Models;

namespace turistico.App_Start
{
    public class DbConfig
    {
        public static void Configure()
        {
            Database.SetInitializer<ApplicationDbContext>(null);
        }
    }
}