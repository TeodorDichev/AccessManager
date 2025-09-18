using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;

namespace AccessManager.Services
{
    public class SeedService
    {
        private readonly IConfiguration _config;
        private readonly PasswordService _passwordService;
        private readonly Context _context;

        public SeedService(IConfiguration config, PasswordService passwordService, Context context)
        {
            _config = config;
            _passwordService = passwordService;
            _context = context;
        }

        public void SeedAdmin()
        {
            // If superadmin already exists, exit
            if (_context.Users.Any(u => u.UserName == "adichev"))
                return;

            // Ensure department exists
            var department = _context.Departments.FirstOrDefault(d => d.Description == "неопределен");
            if (department == null)
            {
                department = new Department { Id = Guid.NewGuid(), Description = "неопределен" };
                _context.Departments.Add(department);
            }

            // Ensure unit exists
            var unit = _context.Units.FirstOrDefault(u => u.Description == "неопределен" && u.DepartmentId == department.Id);
            if (unit == null)
            {
                unit = new Unit { Id = Guid.NewGuid(), Description = "неопределен", DepartmentId = department.Id, Department = department };
                _context.Units.Add(unit);
            }

            // Ensure position exists
            var position = _context.Positions.FirstOrDefault(p => p.Description == "главен администратор");
            if (position == null)
            {
                position = new Position { Id = Guid.NewGuid(), Description = "главен администратор" };
                _context.Positions.Add(position);
            }

            _context.SaveChanges(); // Save to get all IDs

            // Create superadmin user
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "adichev",
                FirstName = "Ангел",
                MiddleName = "Тодоров",
                LastName = "Дичев",
                ReadingAccess = AuthorityType.SuperAdmin,
                WritingAccess = AuthorityType.SuperAdmin,
                UnitId = unit.Id,
                Unit = unit,
                PositionId = position.Id,
                Position = position,
                AccessibleUnits = _context.Units
                    .Select(u => new UnitUser { UserId = Guid.NewGuid(), UnitId = u.Id }) // will fix below
                    .ToList()
            };

            // Correct AccessibleUnits mapping
            user.AccessibleUnits = _context.Units
                .Select(u => new UnitUser { UserId = user.Id, UnitId = u.Id })
                .ToList();

            // Set password
            user.Password = _passwordService.HashPassword(user, _config["SuperAdmin:Password"]);

            _context.Users.Add(user);
            _context.SaveChanges();
        }
    }
}
