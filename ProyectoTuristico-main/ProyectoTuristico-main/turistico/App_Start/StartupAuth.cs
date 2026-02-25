using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using turistico.Models;

namespace turistico.App_Start
{
    public static class StartupAuth
    {
        public static void ConfigureAuth(IAppBuilder app)
        {
            // DbContext por request
            app.CreatePerOwinContext(ApplicationDbContext.Create);

            // UserManager por request
            app.CreatePerOwinContext<UserManager<ApplicationUser>>((options, context) =>
            {
                var db = context.Get<ApplicationDbContext>(); // <- debe funcionar con Identity.Owin
                var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

                manager.PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 6,
                    RequireDigit = false,
                    RequireLowercase = true,
                    RequireUppercase = false,
                    RequireNonLetterOrDigit = false
                };

                return manager;
            });

            // RoleManager por request
            app.CreatePerOwinContext<RoleManager<IdentityRole>>((options, context) =>
            {
                var db = context.Get<ApplicationDbContext>();
                return new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            });

            // Cookies
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                SlidingExpiration = true
            });
        }
    }
}
