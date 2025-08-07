namespace AccessManager.ViewModels.InformationSystem
{
    public class AccessViewModel
    {
        public Guid AccessId { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid DirectiveId { get; set; }
        public string DirectiveDescription { get; set; } = string.Empty;
    }
}
