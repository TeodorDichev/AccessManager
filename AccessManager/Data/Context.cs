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

        public DbSet<InformationSystem> InformationSystems { get; set; } = null!;
        public DbSet<Access> Accesses { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserAccess> UserAccesses { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Unit> Units { get; set; } = null!;
        public DbSet<Log> Logs { get; set; } = null!;
        public DbSet<UnitUser> UnitUser { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureInformationSystem(modelBuilder);
            ConfigureAccess(modelBuilder);
            ConfigureUsers(modelBuilder);
            ConfigureUserAccess(modelBuilder);
            ConfigureDepartment(modelBuilder);
            ConfigureUserUnit(modelBuilder);
            ConfigureUnit(modelBuilder);
            ConfigureLog(modelBuilder);
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
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.AccessibleUnits)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private void ConfigureLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired();

                entity.Property(e => e.CreatedOn)
                    .IsRequired();
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
                    .HasMaxLength(50);

                entity.HasMany(e => e.Units)
                      .WithOne(a => a.Department)
                      .HasForeignKey(a => a.DepartmentId)
                      .IsRequired();
            });
        }

        private void ConfigureUnit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(50);

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
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private void ConfigureInformationSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InformationSystem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasMany(e => e.Accesses)
                      .WithOne(a => a.System)
                      .HasForeignKey(a => a.SystemId)
                      .IsRequired();
            });
        }

        private void ConfigureAccess(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Access>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.Description)
                      .IsUnique();

                entity.HasOne(e => e.System)
                      .WithMany(s => s.Accesses)
                      .HasForeignKey(e => e.SystemId)
                      .IsRequired();

                entity.HasMany(e => e.UserAccesses)
                      .WithOne(ea => ea.Access)
                      .HasForeignKey(ea => ea.AccessId);
            });
        }

        private void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.UserName)
                      .IsUnique();

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.MiddleName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.WritingAccess)
                    .IsRequired();

                entity.Property(e => e.ReadingAccess)
                    .IsRequired();

                entity.HasIndex(e => e.EGN)
                    .IsUnique();

                entity.HasIndex(e => e.Phone)
                    .IsUnique();

                entity.HasMany(e => e.UserAccesses)
                      .WithOne(ea => ea.User)
                      .HasForeignKey(ea => ea.UserId);

                entity.HasMany(e => e.AccessibleUnits)
                    .WithOne(ea => ea.User)
                    .HasForeignKey(ea => ea.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Unit)
                    .WithMany(s => s.UsersFromUnit)
                    .HasForeignKey(s => s.UnitId)
                    .IsRequired();
            });
        }

        private void ConfigureUserAccess(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccess>(entity =>
            {
                entity.HasKey(ua => new { ua.UserId, ua.AccessId });

                entity.HasOne(ua => ua.User)
                    .WithMany(e => e.UserAccesses)
                    .HasForeignKey(ea => ea.UserId)
                    .IsRequired();

                entity.HasOne(ea => ea.Access)
                    .WithMany(a => a.UserAccesses)
                    .HasForeignKey(ea => ea.AccessId)
                    .IsRequired();

                entity.Property(ea => ea.Directive)
                    .IsRequired();

                entity.Property(ea => ea.GrantedOn)
                    .IsRequired();
            });
        }
    }
}
