using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Utills;
using AccessManager.ViewModels.User;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AccessManager.Services
{
    public class FileService
    {
        private readonly Context _context;
        private readonly AccessService _accessService;
        private readonly SeedService _seedService;
        public FileService(Context context, AccessService accessService, SeedService seedService)
        {
            _context = context;
            _accessService = accessService;
            _seedService = seedService;
        }

        internal StringBuilder GetUsersCsv(List<User> accessibleUsers)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Потребителско име,Собствено име,Средно име,Фамилия,Достъп за четене,Достъп за писане,Дирекция,Отдел,ЕГН,Телефон");

            foreach (var u in accessibleUsers.OrderBy(u => u.UserName))
            {
                sb.AppendLine($"\"{u.UserName}\",\"{u.FirstName}\",\"{u.MiddleName}\",\"{u.LastName}\",\"{BulgarianLocalization.GetBulgarianAuthorityType(u.ReadingAccess)}\"," +
                    $"\"{BulgarianLocalization.GetBulgarianAuthorityType(u.WritingAccess)}\",\"{u.Unit.Department.Description}\",\"{u.Unit.Description}\"," +
                    $"\"{u.EGN}\",\"{u.Phone}\"");
            }

            return sb;
        }

        internal StringBuilder GetUserAccessCsv(List<User> accessibleUsers)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Потребителско име,Собствено име,Фамилия,Достъп,Заповед за даване,Спрян,Заповед за спиране");

            foreach (var u in _context.UserAccesses.Where(ua => accessibleUsers.Select(u => u.Id).Contains(ua.UserId)).OrderBy(ua => ua.User.UserName))
            {
                sb.AppendLine($"\"{u.User.UserName}\",\"{u.User.FirstName}\",\"{u.User.LastName}\",\"{u.Access.FullDescription}\"," +
                    $"\"{u.GrantedByDirective.Name}\",\"{(u.RevokedByDirective == null ? "не" : "да")}\",\"{(u.RevokedByDirective == null ? "" : u.RevokedByDirective.Name)}\"");
            }

            return sb;
        }

        internal StringBuilder GetUnitsCsv(List<Unit> accessibleUnits)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Дирекция,Отдел към дирекцията");

            foreach (var u in accessibleUnits.OrderBy(u => u.Department.Description).ThenBy(u => u.Description))
            {
                sb.AppendLine($"\"{u.Department.Description}\",\"{u.Description}\"");
            }

            return sb;
        }

        internal StringBuilder GetAccessesCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Достъп");

            var accesses = _context.Accesses.OrderBy(d => d.Description).ToList();

            var childrenLookup = accesses.GroupBy(a => a.ParentAccessId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.ToList());

            void PrintAccess(Access access)
            {
                sb.AppendLine($"\"{access.FullDescription}\"");

                if (childrenLookup.TryGetValue(access.Id, out var children))
                    foreach (var child in children)
                        PrintAccess(child);
            }

            if (childrenLookup.TryGetValue(Guid.Empty, out var roots))
                foreach (var root in roots)
                    PrintAccess(root);

            return sb;
        }

        internal StringBuilder GetUsersUnitsCsv(List<Unit> accessibleUnits)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Потребителско име,Собствено име,Фамилия,Достъп към отдел,Дирекция на отдела");

            foreach (var u in _context.UnitUsers.Where(u => accessibleUnits.Contains(u.Unit)).OrderBy(u => u.User.UserName).ThenBy(u => u.Unit.Department.Description).ThenBy(u => u.Unit.Description))
            {
                sb.AppendLine($"\"{u.User.UserName}\",\"{u.User.FirstName}\",\"{u.User.LastName}\",\"{u.Unit.Description}\",\"{u.Unit.Department.Description}\"");
            }

            return sb;
        }

        internal void DeleteDb()
        {
            _context.UserAccesses.ExecuteDelete();
            _context.UnitUsers.ExecuteDelete();
            _context.Users.ExecuteDelete();
            _context.Accesses.ExecuteDelete();
            _context.Units.ExecuteDelete();
            _context.Departments.ExecuteDelete();
            _context.SaveChanges();
            _seedService.SeedAdmin();
        }

        internal void UploadCompleteTable(IFormFile file, bool drop)
        {
            List<TempUserRecord> tempUsers;
            List<string> parentAccessColumns;

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                {
                    BadDataFound = null,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    IgnoreBlankLines = true
                };
                using var csv = new CsvHelper.CsvReader(reader, config);

                csv.Read();
                csv.ReadHeader();
                var headerRow = csv.HeaderRecord;

                // First 7 columns are user info, rest are parent accesses
                parentAccessColumns = headerRow.Skip(7).Select(h => h.Trim()).ToList();

                tempUsers = new List<TempUserRecord>();

                while (csv.Read())
                {
                    var user = new TempUserRecord
                    {
                        UserName = csv.GetField("UserName")?.Trim() ?? "",
                        FirstName = csv.GetField("FirstName")?.Trim() ?? "",
                        MiddleName = csv.GetField("MiddleName")?.Trim() ?? "",
                        LastName = csv.GetField("LastName")?.Trim() ?? "",
                        Department = csv.GetField("Department")?.Trim() ?? "",
                        Unit = csv.GetField("Unit")?.Trim() ?? "",
                        Position = csv.GetField("Position")?.Trim() ?? ""
                    };

                    user.Accesses = new Dictionary<string, string>();
                    foreach (var parentAccessName in parentAccessColumns)
                    {
                        var val = csv.GetField(parentAccessName)?.Trim();
                        if (string.IsNullOrEmpty(val)) continue;
                        user.Accesses[parentAccessName] = val;
                    }

                    tempUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("CSV parsing error", ex);
            }

            using var tx = _context.Database.BeginTransaction();
            try
            {
                if (drop)
                {
                    _context.UserAccesses.ExecuteDelete();
                    _context.UnitUsers.ExecuteDelete();   
                    _context.Users.ExecuteDelete();
                    _context.Accesses.ExecuteDelete();
                    _context.Units.ExecuteDelete();
                    _context.Departments.ExecuteDelete();
                    _context.SaveChanges();
                }

                // Cache existing entities, trimming keys
                var existingDepartments = _context.Departments.ToDictionary(d => d.Description.Trim());
                var existingUnits = _context.Units.ToDictionary(u => $"{u.Description.Trim()}||{u.DepartmentId}");
                var existingPositions = _context.Positions.ToDictionary(p => p.Description.Trim());
                var existingUsers = _context.Users.ToDictionary(u => u.UserName.Trim());
                var existingAccesses = _context.Accesses.ToDictionary(a => a.Description.Trim());
                var existingDirectives = _context.Directives.ToDictionary(d => d.Name.Trim());

                // Ensure "неопределена" directive
                if (!existingDirectives.ContainsKey("неопределен"))
                {
                    var undefDir = new Directive { Name = "неопределен" };
                    _context.Directives.Add(undefDir);
                    _context.SaveChanges();
                    existingDirectives["неопределен"] = undefDir;
                }

                // Ensure parent accesses exist
                foreach (var parentName in parentAccessColumns)
                {
                    var trimmedParent = parentName.Trim();
                    if (!existingAccesses.ContainsKey(trimmedParent))
                    {
                        var parent = new Access { Description = trimmedParent, FullDescription = trimmedParent, Level = 0 };
                        _context.Accesses.Add(parent);
                        _context.SaveChanges();
                        existingAccesses[trimmedParent] = parent;
                    }
                }

                // Process each user
                foreach (var row in tempUsers)
                {
                    var deptName = string.IsNullOrWhiteSpace(row.Department) ? "неопределен" : row.Department.Trim();
                    if (!existingDepartments.ContainsKey(deptName))
                    {
                        var dept = new Department { Description = deptName };
                        _context.Departments.Add(dept);
                        _context.SaveChanges();
                        existingDepartments[deptName] = dept;
                    }

                    var unitName = string.IsNullOrWhiteSpace(row.Unit) ? "неопределен" : row.Unit.Trim();
                    var unitKey = $"{unitName}||{existingDepartments[deptName].Id}";
                    if (!existingUnits.ContainsKey(unitKey))
                    {
                        var unit = new Unit { Description = unitName, DepartmentId = existingDepartments[deptName].Id };
                        _context.Units.Add(unit);
                        _context.SaveChanges();
                        existingUnits[unitKey] = unit;
                    }

                    var posName = string.IsNullOrWhiteSpace(row.Position) ? "неопределен" : row.Position.Trim();
                    if (!existingPositions.ContainsKey(posName))
                    {
                        var pos = new Position { Description = posName };
                        _context.Positions.Add(pos);
                        _context.SaveChanges();
                        existingPositions[posName] = pos;
                    }

                    // Skip existing users
                    var trimmedUserName = row.UserName?.Trim();
                    if (existingUsers.ContainsKey(trimmedUserName))
                        continue;

                    var user = new User
                    {
                        UserName = row.UserName,
                        FirstName = row.FirstName,
                        MiddleName = row.MiddleName,
                        LastName = row.LastName,
                        UnitId = existingUnits[unitKey].Id,
                        PositionId = existingPositions[posName].Id,
                        EGN = null,
                        Phone = null,
                        WritingAccess = Data.Enums.AuthorityType.None,
                        ReadingAccess = Data.Enums.AuthorityType.None,
                    };
                    _context.Users.Add(user);
                    _context.SaveChanges();
                    existingUsers[user.UserName] = user;

                    // Map subaccesses
                    foreach (var parentAccessName in parentAccessColumns)
                    {
                        if (!row.Accesses.TryGetValue(parentAccessName, out var subVal) || string.IsNullOrWhiteSpace(subVal))
                            continue;

                        subVal = subVal.Trim();
                        var parentAccess = existingAccesses[parentAccessName.Trim()];

                        var subKey = $"{subVal}||{parentAccess.Id}";
                        if (!existingAccesses.ContainsKey(subKey))
                        {
                            var subAccess = new Access
                            {
                                Description = subVal,
                                ParentAccessId = parentAccess.Id,
                                FullDescription = _accessService.GenerateAccessFullDescription(subVal, parentAccess.Id),
                                Level = parentAccess.Level + 1
                            };
                            _context.Accesses.Add(subAccess);
                            _context.SaveChanges();
                            existingAccesses[subKey] = subAccess;
                        }

                        // Link user to subaccess
                        var userAccess = new UserAccess
                        {
                            UserId = user.Id,
                            AccessId = existingAccesses[subKey].Id,
                            GrantedByDirective = existingDirectives["неопределен"],
                            GrantedOn = DateTime.UtcNow
                        };
                        _context.UserAccesses.Add(userAccess);
                    }

                    _context.SaveChanges();
                }

                _seedService.SeedAdmin();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
