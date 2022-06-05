using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Delineation.Models;
using Microsoft.AspNetCore.Identity;
using CustomIdentity.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Delineation.Services;
using Delineation.Data;
// hello world
namespace Delineation
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<EmailService>();
            string connString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<DelineationContext>(options => options.UseSqlite(connString));
            //identity
            services.AddIdentity<User, IdentityRole>(oopt =>
            {
                oopt.Password.RequiredLength = 4;   // минимальна€ длина
                oopt.Password.RequireNonAlphanumeric = false;   // требуютс€ ли не алфавитно-цифровые символы
                oopt.Password.RequireLowercase = false; // требуютс€ ли символы в нижнем регистре
                oopt.Password.RequireUppercase = false; // требуютс€ ли символы в верхнем регистре
                oopt.Password.RequireDigit = false; // требуютс€ ли цифры
                //oopt.User.AllowedUserNameCharacters = null; // допускает любые символы в имени юсера
                oopt.User.AllowedUserNameCharacters = "абвгдеЄжзийклмнопрстуфхцчщшъыьэю€јЅ¬√ƒ≈®∆«»… ЋћЌќѕ–—“”‘’÷„ўЎЏџ№Ёёя- ";
            })
                .AddEntityFrameworkStores<DelineationContext>()
                .AddDefaultTokenProviders();
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                //options.Cookie.Name = "YourAppCookieName";
                //options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Identity/Account/Login";
            });
            //identity--end
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAreaControllerRoute(
                    name: "Identity_area",
                    areaName: "Identity",
                    pattern: "identity/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
