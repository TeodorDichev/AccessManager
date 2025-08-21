namespace AccessManager.ViewModels
{
    public interface IPagedResult
    {
        int Page { get; }
        int TotalPages { get; }
    }
}
