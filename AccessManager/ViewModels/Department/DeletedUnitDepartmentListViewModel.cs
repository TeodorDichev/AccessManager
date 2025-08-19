namespace AccessManager.ViewModels.Department
{
    public class DeletedUnitDepartmentListViewModel
    {
        public List<UnitDepartmentViewModel> UnitDepartments { get; set; } = [];
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
