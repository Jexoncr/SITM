using Microsoft.Owin;
using Owin;
using turistico.App_Start;

[assembly: OwinStartup(typeof(turistico.Startup))]

namespace turistico
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            App_Start.StartupAuth.ConfigureAuth(app);

            // ✅ Crear roles y usuario admin
            IdentitySeed.CreateRolesAndAdmin();
        }
    }
}
