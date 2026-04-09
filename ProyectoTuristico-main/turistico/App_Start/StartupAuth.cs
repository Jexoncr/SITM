using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using turistico.Models;


namespace turistico.App_Start
{
    public class StartupAuth
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        public static void ConfigureAuth(IAppBuilder app)
        {
            app.CreatePerOwinContext(ApplicationDbContext.Create);

            app.CreatePerOwinContext<Microsoft.AspNet.Identity.UserManager<ApplicationUser>>((options, context) =>
            {
                var db = context.Get<ApplicationDbContext>();
                var manager = new Microsoft.AspNet.Identity.UserManager<ApplicationUser>(
                    new UserStore<ApplicationUser>(db)
                );

                manager.PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 6,
                    RequireDigit = false,
                    RequireLowercase = true,
                    RequireUppercase = false,
                    RequireNonLetterOrDigit = false
                };

                manager.UserValidator = new UserValidator<ApplicationUser>(manager)
                {
                    AllowOnlyAlphanumericUserNames = false,
                    RequireUniqueEmail = true
                };

                return manager;
            });

            app.CreatePerOwinContext<Microsoft.AspNet.Identity.RoleManager<IdentityRole>>((options, context) =>
            {
                var db = context.Get<ApplicationDbContext>();
                return new Microsoft.AspNet.Identity.RoleManager<IdentityRole>(
                    new RoleStore<IdentityRole>(db)
                );
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                SlidingExpiration = true
            });
        }
    }
}