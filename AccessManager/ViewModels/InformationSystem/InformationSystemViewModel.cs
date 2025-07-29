namespace AccessManager.ViewModels.InformationSystem
{
    public class InformationSystemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string? Directive { get; set; }
        public List<AccessViewModel> Accesses { get; set; } = [];
    }
}
