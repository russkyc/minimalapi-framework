namespace Russkyc.MinimalApi.Framework.Core;

public class PaginatedCollection<T> : IPaginatedCollection<T> where T : class
{
    public required ICollection<T> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
}