using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using turistico.Models;

namespace turistico.App_Start
{
    public static class IdentitySeed
    {
        public static void CreateRolesAndAdmin()
        {
            using (var db = new ApplicationDbContext())
            {
                var roleManager = new RoleManager<IdentityRole>(
                    new RoleStore<IdentityRole>(db)
                );

                var userManager = new UserManager<ApplicationUser>(
                    new UserStore<ApplicationUser>(db)
                );

                // Roles
                if (!roleManager.RoleExists("Admin"))
                    roleManager.Create(new IdentityRole("Admin"));

                if (!roleManager.RoleExists("Cliente"))
                    roleManager.Create(new IdentityRole("Cliente"));

                // Admin
                var adminEmail = "admin@turistico.com";
                var adminPassword = "Admin123!";

                var admin = userManager.FindByEmail(adminEmail);

                if (admin == null)
                {
                    admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        Nombre = "Admin",
                        Apellido = "SITM",
                        PhoneNumber = "00000000"
                    };

                    var result = userManager.Create(admin, adminPassword);
                    if (!result.Succeeded) return;
                }
                else
                {
                    // ✅ Si ya existe, actualizar datos (por si estaban NULL)
                    admin.Nombre = admin.Nombre ?? "Admin";
                    admin.Apellido = admin.Apellido ?? "SITM";
                    admin.PhoneNumber = admin.PhoneNumber ?? "00000000";

                    userManager.Update(admin);
                }

                // Rol Admin
                if (!userManager.IsInRole(admin.Id, "Admin"))
                    userManager.AddToRole(admin.Id, "Admin");

                // (Opcional) Usuario Cliente de prueba
                var clienteEmail = "cliente@turistico.com";
                var clientePassword = "Cliente123!";

                var cliente = userManager.FindByEmail(clienteEmail);
                if (cliente == null)
                {
                    cliente = new ApplicationUser
                    {
                        UserName = clienteEmail,
                        Email = clienteEmail,
                        Nombre = "Cliente",
                        Apellido = "Demo",
                        PhoneNumber = "88888888"
                    };

                    var resCliente = userManager.Create(cliente, clientePassword);
                    if (resCliente.Succeeded)
                        userManager.AddToRole(cliente.Id, "Cliente");
                }
            }
        }
    }
}
