using AccessManager.Data.Enums;

namespace AccessManager.Data.Entities
{
    public class Log
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public LogAction ActionType { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
