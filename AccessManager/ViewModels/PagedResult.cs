using AccessManager.Utills;

namespace AccessManager.ViewModels
{
    public class PagedResult<T> : IPagedResult
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public string PageParam { get; set; }
        public int PageSize { get; set; } = Constants.ItemsPerPage;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
