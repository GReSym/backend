using GReSym.Core.Common;

namespace GReSym.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    IEnumerable<T> GetAll();
    Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize=Constants.Api.DefaultPageSize);
    Task AddAsync(T entity);
    void Update(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}