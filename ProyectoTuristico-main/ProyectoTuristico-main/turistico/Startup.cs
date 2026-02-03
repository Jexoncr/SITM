using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(turistico.Startup))]

namespace turistico
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            App_Start.StartupAuth.ConfigureAuth(app);
        }
    }
}
