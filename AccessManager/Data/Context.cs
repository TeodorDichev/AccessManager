using AccessManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessManager.Data
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options)
            : base(options)
        {
        }

        public DbSet<Access> Accesses { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserAccess> UserAccesses { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Unit> Units { get; set; } = null!;
        public DbSet<Log> Logs { get; set; } = null!;
        public DbSet<UnitUser> UnitUsers { get; set; } = null!;
        public DbSet<Directive> Directives { get; set; } = null!;
        public DbSet<Position> Positions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigurePosition(modelBuilder);
            ConfigureAccess(modelBuilder);
            ConfigureUsers(modelBuilder);
            ConfigureUserAccess(modelBuilder);
            ConfigureDepartment(modelBuilder);
            ConfigureUserUnit(modelBuilder);
            ConfigureUnit(modelBuilder);
            ConfigureLog(modelBuilder);
            ConfigureDirective(modelBuilder);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        private void ConfigureUserUnit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnitUser>(entity =>
            {
                entity.HasKey(e => new { e.UnitId, e.UserId });

                entity.HasOne(e => e.Unit)
                      .WithMany(u => u.UsersWithAccess)
                      .HasForeignKey(e => e.UnitId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.AccessibleUnits)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false);
            });
        }

        private void ConfigurePosition(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Position>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired();
            });
        }

        private void ConfigureLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired();

                entity.Property(e => e.ActionType)
                    .IsRequired();

                entity.Property(e => e.Date)
                    .IsRequired();
            });
        }

        private void ConfigureDirective(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Directive>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired();
                entity.HasIndex(e => e.Name)
                     .IsUnique();

                entity.HasQueryFilter(e => e.DeletedOn == null);
            });
        }

        private void ConfigureDepartment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Description)
                      .IsUnique();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasMany(e => e.Units)
                      .WithOne(a => a.Department)
                      .HasForeignKey(a => a.DepartmentId)
                      .IsRequired();

                entity.HasQueryFilter(e => e.DeletedOn == null);
            });
        }

        private void ConfigureUnit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(e => new { e.Description, e.DepartmentId })
                    .IsUnique();

                entity.HasOne(e => e.Department)
                      .WithMany(s => s.Units)
                      .HasForeignKey(s => s.DepartmentId)
                      .IsRequired();

                entity.HasMany(e => e.UsersFromUnit)
                    .WithOne(u => u.Unit)
                    .HasForeignKey(u => u.UnitId);

                entity.HasMany(e => e.UsersWithAccess)
                    .WithOne(uu => uu.Unit)
                    .HasForeignKey(uu => uu.UnitId)
                    .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired(false);
                ;

                entity.HasQueryFilter(e => e.DeletedOn == null);
            });
        }

        private void ConfigureAccess(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Access>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FullDescription)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.HasMany(e => e.UserAccesses)
                      .WithOne(ea => ea.Access)
                      .HasForeignKey(ea => ea.AccessId);

                modelBuilder.Entity<Access>()
                    .HasOne(a => a.ParentAccess)
                    .WithMany(a => a.SubAccesses)
                    .HasForeignKey(a => a.ParentAccessId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => e.DeletedOn == null);
            });
        }

        private void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasIndex(e => e.UserName)
                      .IsUnique();

                entity.Property(e => e.FirstName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.MiddleName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.LastName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.WritingAccess)
                      .IsRequired();

                entity.Property(e => e.ReadingAccess)
                      .IsRequired();

                entity.HasIndex(e => e.EGN).IsUnique().HasFilter("[EGN] IS NOT NULL"); ;
                entity.HasIndex(e => e.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL"); ;

                entity.HasMany(e => e.UserAccesses)
                      .WithOne(ea => ea.User)
                      .HasForeignKey(ea => ea.UserId); // simple, no duplicate

                entity.HasOne(e => e.Unit)
                      .WithMany(s => s.UsersFromUnit)
                      .HasForeignKey(s => s.UnitId)
                      .IsRequired();

                entity.HasQueryFilter(e => e.DeletedOn == null);
            });
        }

        private void ConfigureUserAccess(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccess>(entity =>
            {
                entity.HasKey(ua => new { ua.UserId, ua.AccessId });

                entity.HasOne(ua => ua.User)
                    .WithMany(u => u.UserAccesses)
                    .HasForeignKey(ua => ua.UserId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);

                entity.HasOne(ua => ua.Access)
                    .WithMany(a => a.UserAccesses)
                    .HasForeignKey(ua => ua.AccessId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);

                entity.HasOne(ua => ua.GrantedByDirective)
                    .WithMany()
                    .HasForeignKey(ua => ua.GrantedByDirectiveId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);

                entity.HasOne(ua => ua.GrantedByDirective);
            });
        }
    }
}
