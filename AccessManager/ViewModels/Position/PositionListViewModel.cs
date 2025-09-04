namespace AccessManager.ViewModels.Position
{
    public class PositionListViewModel : IAuthAwareViewModel
    {
        public PagedResult<Data.Entities.Position> Positions { get; set; } = new ();
    }
}
