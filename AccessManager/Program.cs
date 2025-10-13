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
            // possible fix for NAS server error
            string basedir = AppDomain.CurrentDomain.BaseDirectory;

            WebApplicationOptions options = new()
            {
                ContentRootPath = basedir,
                Args = args,
                WebRootPath = Path.Combine(basedir, "wwwroot")
            };

            var builder = WebApplication.CreateBuilder(options);

            string connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"] ?? throw new ArgumentException("Connection string not found");
            builder.Services.AddDbContext<Context>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddScoped<LogService>();
            builder.Services.AddScoped<UnitService>();
            builder.Services.AddScoped<FileService>();
            builder.Services.AddScoped<SeedService>();
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

            //Add before deploying
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, 5000);
            });

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<Context>();

                // creates the database
                context.Database.Migrate();

                var seedService = services.GetRequiredService<SeedService>();
                seedService.SeedAdmin();

                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }

                //app.UseHttpsRedirection();

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
}
