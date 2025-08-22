namespace AccessManager.ViewModels.Directive
{
    public class DirectiveListViewModel
    {
        public PagedResult<AccessManager.Data.Entities.Directive> Directives { get; set; } = new();
    }
}
