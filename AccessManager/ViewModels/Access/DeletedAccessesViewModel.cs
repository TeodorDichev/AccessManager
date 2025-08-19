namespace AccessManager.ViewModels.Access
{
    public class DeletedAccessesViewModel
    {
        public List<AccessListItemViewModel> Accesses { get; set; } = [];

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
