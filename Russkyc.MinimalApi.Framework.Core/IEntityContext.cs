namespace Russkyc.MinimalApi.Framework.Core;

public interface IEntityContext<TModel>
    where TModel : class
{
    IQueryable<TModel> Set();
    ValueTask<int> AddAsync(TModel model);
    ValueTask<int> RemoveAsync(TModel model);
    ValueTask<int> UpdateAsync(TModel model);
}