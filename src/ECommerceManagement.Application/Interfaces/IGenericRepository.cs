using System.Linq.Expressions;

namespace ECommerceManagement.Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    // Yeni eklenen: Include destekli GetById
    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object?>>[] includes);
    
    Task<IEnumerable<T>> GetAllAsync();
    // Yeni eklenen: Include destekli GetAll
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object?>>[] includes);
    
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}