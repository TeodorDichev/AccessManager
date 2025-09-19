namespace AccessManager.ViewModels
{
    public interface IPagedResult
    {
        int Page { get; }
        string PageParam { get; }
        int TotalPages { get; }
    }
}
