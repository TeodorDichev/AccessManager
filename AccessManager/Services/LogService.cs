using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;

namespace AccessManager.Services
{
    public class LogService
    {
        private readonly Context _context;
        public LogService(Context context)
        {
            _context = context;
        }

        public List<Log> GetLogs()
        {
            return _context.Logs.ToList();
        }
        public void AddLog(User author, LogAction logType, Department department)
        {
            AddLog(new Log() { Description = $"{author.UserName} {logType} дирекция {department.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, Unit unit)
        {
            AddLog(new Log() { Description = $"{author.UserName} {logType} отдел {unit.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, Access access)
        {
            AddLog(new Log() { Description = $"{author.UserName} {logType} достъп {access.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, Directive directive)
        {
            AddLog(new Log() { Description = $"{author.UserName} {logType} заповед {directive.Name}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, UnitUser uu)
        {
            AddLog(new Log() { Description = $"{author.UserName} {logType} достъп на {uu.User.UserName} до отдел {uu.Unit.Description}", ActionType = logType });
        }
        public void AddLog(User author, LogAction logType, UserAccess ua)
        {
            AddLog(new Log() { Description = $"{author.UserName} {logType} достъп на {ua.User.UserName} до система {ua.Access.Description}", ActionType = logType });
        }
        private void AddLog(Log log)
        {
            _context.Add(log);
            _context.SaveChanges();
        }
    }
}
