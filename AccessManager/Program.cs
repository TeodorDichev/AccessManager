using AccessManager.Data;
using AccessManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AccessManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<LogService>();
            builder.Services.AddScoped<UnitService>();
            builder.Services.AddScoped<FileService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AccessService>();
            builder.Services.AddScoped<PositionService>();
            builder.Services.AddScoped<PasswordService>();
            builder.Services.AddScoped<DirectiveService>();
            builder.Services.AddScoped<UserAccessService>();
            builder.Services.AddScoped<DepartmentService>();

            // Add services to the container.
            builder.Services.AddSession();
            builder.Services.AddControllersWithViews();

            // Add before deploying
            //builder.WebHost.ConfigureKestrel(options =>
            //{
            //    options.Listen(IPAddress.Any, 5000); 
            //});

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<Context>();

                // creates the database
                context.Database.Migrate();

                var config = services.GetRequiredService<IConfiguration>();
                var passwordService = services.GetRequiredService<PasswordService>();

                SeedData.Seed(context, config, passwordService);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();
            app.MapGet("/Home", () => "OK");
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
