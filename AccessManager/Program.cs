using AccessManager.Data;
using AccessManager.Services;
using Microsoft.EntityFrameworkCore;

namespace AccessManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddSingleton<PasswordService>();
            builder.Services.AddSingleton<UserService>();

            // Add services to the container.
            builder.Services.AddSession();
            builder.Services.AddControllersWithViews();

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<Context>();
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

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
