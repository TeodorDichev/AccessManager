namespace AccessManager.Data.Entities
{
    public class EmployeeAccess
    {
        public Guid EmployeeId { get; set; }
        virtual public Employee Employee { get; set; } = null!;

        public Guid AccessId { get; set; }
        virtual public Access Access { get; set; } = null!;

        public string Directive { get; set; } = string.Empty;

        public DateTime AccessGrantedDate { get; set; }
    }
}
