namespace Russkyc.MinimalApi.Framework;

public class PaginatedCollection<T> where T : class
{
    public required ICollection<T> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
}