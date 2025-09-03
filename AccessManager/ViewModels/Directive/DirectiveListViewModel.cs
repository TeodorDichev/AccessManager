namespace AccessManager.ViewModels.Directive
{
    public class DirectiveListViewModel : IAuthAwareViewModel
    {
        public PagedResult<Data.Entities.Directive> Directives { get; set; } = new ();
    }
}
