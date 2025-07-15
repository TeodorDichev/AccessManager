using AccessManager.Data.Entities;
using AccessManager.Services;
using Microsoft.AspNetCore.Identity;

namespace AccessManager.Data
{
    public class SeedData
    {
        public static void Seed(Context context, IConfiguration config, PasswordService passwordService)
        {
            if (context.Users.FirstOrDefault(d => d.UserName == "adichev") == null)
            {
                var user = new User
                {
                    UserName = "adichev",
                    FirstName = "Ангел",
                    MiddleName = "Тодоров",
                    LastName = "Дичев",
                    ReadingAccess = Data.Enums.ReadingAccess.Full,
                    WritingAccess = Data.Enums.WritingAccess.Full,
                    Password = passwordService.HashPassword(config["SuperAdmin:Password"]),
                };

                if (context.Departments.FirstOrDefault(d => d.Description == "ДКИС") == null)
                {
                    var department = new Department
                    {
                        Id = Guid.NewGuid(),
                        Description = "ДКИС"
                    };

                    var unit = new Unit
                    {
                        Id = Guid.NewGuid(),
                        Description = "Пазарджик",
                        DepartmentId = department.Id,
                        Department = department
                    };

                    user.UnitId = unit.Id;
                    user.Unit = unit;

                    context.Departments.Add(department);
                    context.Units.Add(unit);
                    context.Users.Add(user);
                }
                else
                {
                    var department = context.Departments.FirstOrDefault(d => d.Description == "ДКИС")!;

                    if (context.Units.FirstOrDefault(d => d.Description == "Пазарджик" && d.Department.Description == "ДКИС") == null)
                    {
                        var unit = new Unit
                        {
                            Id = Guid.NewGuid(),
                            Description = "Пазарджик",
                            DepartmentId = department.Id,
                            Department = department
                        };

                        user.UnitId = unit.Id;
                        user.Unit = unit;

                        context.Units.Add(unit);
                        context.Users.Add(user);
                    }
                    else                    
                    {
                        var unit = context.Units.FirstOrDefault(d => d.Description == "Пазарджик" && d.Department.Description == "ДКИС")!;
                        user.UnitId = unit.Id;
                        user.Unit = unit;
                        context.Users.Add(user);
                    }
                }

                context.SaveChanges();
            }
        }
    }
}
