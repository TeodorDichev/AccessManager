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
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<EmployeeAccess> EmployeeAccesses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureInformationSystem(modelBuilder);
            ConfigureAccess(modelBuilder);
            ConfigureAdmin(modelBuilder);
            ConfigureEmployee(modelBuilder);
            ConfigureEmployeeAccess(modelBuilder);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
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

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.UserName)
                      .IsUnique();

                entity.HasOne(e => e.System)
                      .WithMany(s => s.Accesses)
                      .HasForeignKey(e => e.SystemId)
                      .IsRequired();

                entity.HasMany(e => e.EmployeeAccesses)
                      .WithOne(ea => ea.Access)
                      .HasForeignKey(ea => ea.AccessId);
            });
        }

        private void ConfigureAdmin(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Admin>(entity =>
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

                entity.Property(e => e.Phone)
                    .IsRequired();

                entity.HasIndex(e => e.Phone)
                    .IsUnique();

                entity.Property(e => e.Role)
                    .IsRequired();
            });
        }

        private void ConfigureEmployee(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
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

                entity.Property(e => e.Department)
                    .IsRequired();

                entity.Property(e => e.EGN)
                    .IsRequired();

                entity.HasIndex(e => e.EGN)
                    .IsUnique();

                entity.Property(e => e.Phone)
                    .IsRequired();

                entity.HasIndex(e => e.Phone)
                    .IsUnique();

                entity.HasMany(e => e.EmployeeAccesses)
                      .WithOne(ea => ea.Employee)
                      .HasForeignKey(ea => ea.EmployeeId);
            });
        }

        private void ConfigureEmployeeAccess(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmployeeAccess>(entity =>
            {
                entity.HasKey(ea => new { ea.EmployeeId, ea.AccessId });

                entity.HasOne(ea => ea.Employee)
                    .WithMany(e => e.EmployeeAccesses)
                    .HasForeignKey(ea => ea.EmployeeId)
                    .IsRequired();

                entity.HasOne(ea => ea.Access)
                    .WithMany(a => a.EmployeeAccesses)
                    .HasForeignKey(ea => ea.AccessId)
                    .IsRequired();

                entity.Property(ea => ea.Directive)
                    .IsRequired();

                entity.Property(ea => ea.AccessGrantedDate)
                    .IsRequired();
            });
        }
    }
}
