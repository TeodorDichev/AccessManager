namespace AccessManager.ViewModels.Log
{
    public class LogListViewModel
    {
        public List<AccessManager.Data.Entities.Log> Logs { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }
}
