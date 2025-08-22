namespace AccessManager.ViewModels.Access
{
    public class DeletedAccessesViewModel
    {
        public PagedResult<AccessListItemViewModel> Accesses { get; set; } = new();
    }
}
