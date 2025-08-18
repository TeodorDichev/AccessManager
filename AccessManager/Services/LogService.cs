using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Utills;

namespace AccessManager.Services
{
    public class LogService
    {
        private readonly Context _context;
        public LogService(Context context)
        {
            _context = context;
        }

        public List<Log> GetLogs(int page)
        {
            return _context.Logs.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).OrderBy(l => l.Date).ToList();
        }
        public int GetLogsCount()
        {
            return _context.Logs.Count();
        }
        public void AddLog(User author, LogAction logType, User user)
        {
            AddLog(new Log() { Description = $"{author.UserName} {BulgarianLocalization.GetBulgarianLogAction(logType)} потребител {user.UserName}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, Department department)
        {
            AddLog(new Log() { Description = $"{author.UserName} {BulgarianLocalization.GetBulgarianLogAction(logType)} дирекция {department.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, Unit unit)
        {
            AddLog(new Log() { Description = $"{author.UserName} {BulgarianLocalization.GetBulgarianLogAction(logType)} отдел {unit.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, Access access)
        {
            AddLog(new Log() { Description = $"{author.UserName} {BulgarianLocalization.GetBulgarianLogAction(logType)} достъп {access.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, Directive directive)
        {
            AddLog(new Log() { Description = $"{author.UserName} {BulgarianLocalization.GetBulgarianLogAction(logType)} заповед {directive.Name}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, UnitUser uu)
        {
            AddLog(new Log() { Description = $"{author.UserName} {BulgarianLocalization.GetBulgarianLogAction(logType)} достъп на {uu.User.UserName} до отдел {uu.Unit.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, UserAccess ua)
        {
            AddLog(new Log()
            {
                Description = $"{author.UserName} {BulgarianLocalization.GetBulgarianLogAction(logType)} достъп на {ua.User.UserName} " +
                $"до система {ua.Access.Description} чрез заповед {(ua.RevokedByDirective == null ? ua.GrantedByDirective.Name : ua.RevokedByDirective.Name)}",
                ActionType = logType
            });
        }
        private void AddLog(Log log)
        {
            _context.Add(log);
            _context.SaveChanges();
        }

        internal void DeleteAllLogs()
        {
            var allItems = _context.Logs.ToList();
            _context.Logs.RemoveRange(allItems);
            _context.SaveChanges();
        }
    }
}
