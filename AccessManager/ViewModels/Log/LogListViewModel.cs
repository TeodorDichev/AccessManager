namespace AccessManager.ViewModels.Log
{
    public class LogListViewModel
    {
        public PagedResult<AccessManager.Data.Entities.Log> Logs { get; set; } = [];
    }
}
