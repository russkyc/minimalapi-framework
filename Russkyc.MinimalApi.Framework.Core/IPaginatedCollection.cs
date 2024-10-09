namespace Russkyc.MinimalApi.Framework.Core;

public interface IPaginatedCollection<T> where T : class
{
    ICollection<T> Data { get; set; }
    int Page { get; set; }
    int PageSize { get; set; }
    int TotalRecords { get; set; }
    int TotalPages { get; set; }
}