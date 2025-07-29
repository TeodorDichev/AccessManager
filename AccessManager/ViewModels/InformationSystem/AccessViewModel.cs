namespace AccessManager.ViewModels.InformationSystem
{
    public class AccessViewModel
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string? Directive { get; set; }
        public List<AccessViewModel> SubAccesses { get; set; } = [];
    }
}
