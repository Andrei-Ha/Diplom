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
using CustomIdentity.Models;
using CustomIdentity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomIdentity
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddScoped<EmailService>();
            //string connString = Configuration.GetConnectionString("MSsqlPirr2n");
            //string connString = Configuration.GetConnectionString("ConnectionAndr-SQL");
            string connString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationContext>(options =>
                options.UseSqlite(connString));
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
                .AddEntityFrameworkStores<ApplicationContext>()
                .AddDefaultTokenProviders();
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                //options.Cookie.Name = "YourAppCookieName";
                //options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Identity/Account/Login";
            });
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
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
