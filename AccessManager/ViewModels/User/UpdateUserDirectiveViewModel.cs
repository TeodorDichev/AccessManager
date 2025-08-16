namespace AccessManager.ViewModels.User
{
    public class UpdateUserDirectiveViewModel
    {
        public string username { get; set; }
        public Guid UserId { get; set; }
        public Guid AccessId { get; set; }
        public Guid DirectiveId { get; set; }
    }
}
