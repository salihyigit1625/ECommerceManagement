using System.Linq.Expressions;

namespace ECommerceManagement.Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object?>>[] includes);
    
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object?>>[] includes);
    
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int pageNumber = 1,
        int pageSize = 10,
        params System.Linq.Expressions.Expression<Func<T, object>>[] includes);
    
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);    
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}