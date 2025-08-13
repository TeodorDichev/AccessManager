namespace AccessManager.ViewModels.Directive
{
    public class UpdateUserAccessDirectiveViewModel
    {
        public Guid AccessId { get; set; }
        public Guid DirectiveId { get; set; }
        public string Username { get; set; } = null!;
    }
}
