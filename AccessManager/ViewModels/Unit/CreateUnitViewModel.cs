namespace AccessManager.ViewModels.Unit
{
    public class CreateUnitViewModel : IAuthAwareViewModel
    {
        public string UnitName { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public string DepartmentDescription { get; set; } = string.Empty;
    }
}
