// ASP.NET Core startup configuration for MixERP.Net.FrontEnd
// Migrated from ASP.NET Web Forms to ASP.NET Core (net10.0)

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SystemWebAdapters;
using System.Threading.Tasks;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    /// ASP.NET Core web application startup helper.
    /// This replaces the Global.asax and Web.config startup configuration.
    /// </summary>
    public static class WebApplicationStartup
    {
        public static Microsoft.AspNetCore.Builder.WebApplication CreateAndConfigureApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add System.Web adapters for backward compatibility during migration
            builder.Services.AddSystemWebAdapters();

            // Add session support (replacing System.Web.SessionState)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = ".MixERP.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add HTTP context accessor
            builder.Services.AddHttpContextAccessor();

            // Add controllers for API endpoints
            builder.Services.AddControllersWithViews();

            // Add Razor Pages for Web Forms migration
            builder.Services.AddRazorPages();

            // Add memory cache
            builder.Services.AddMemoryCache();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Site/RuntimeError");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Use System.Web adapters middleware
            app.UseSystemWebAdapters();

            app.UseAuthentication();
            app.UseAuthorization();

            // Use session
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            // Default redirect to SignIn
            app.MapGet("/", (HttpContext ctx) => {
                ctx.Response.Redirect("/SignIn.aspx");
                return Task.CompletedTask;
            });

            return app;
        }
    }
}
