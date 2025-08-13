namespace AccessManager.ViewModels.Directive
{
    public class DirectiveListViewModel
    {
        public List<AccessManager.Data.Entities.Directive> Directives { get; set; } = [];
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
