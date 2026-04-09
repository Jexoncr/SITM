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

                if (!roleManager.RoleExists("Admin"))
                    roleManager.Create(new IdentityRole("Admin"));

                if (!roleManager.RoleExists("Cliente"))
                    roleManager.Create(new IdentityRole("Cliente"));

                if (!roleManager.RoleExists("Comercio"))
                    roleManager.Create(new IdentityRole("Comercio"));

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
                    admin.Nombre = string.IsNullOrWhiteSpace(admin.Nombre) ? "Admin" : admin.Nombre;
                    admin.Apellido = string.IsNullOrWhiteSpace(admin.Apellido) ? "SITM" : admin.Apellido;
                    admin.PhoneNumber = string.IsNullOrWhiteSpace(admin.PhoneNumber) ? "00000000" : admin.PhoneNumber;
                    userManager.Update(admin);
                }

                if (!userManager.IsInRole(admin.Id, "Admin"))
                    userManager.AddToRole(admin.Id, "Admin");

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