namespace AccessManager.ViewModels.User
{
    public class UpdateUserDirectiveViewModel
    {
        public Guid UserId { get; set; }
        public Guid AccessId { get; set; }
        public Guid? DirectiveId { get; set; }
    }
}
