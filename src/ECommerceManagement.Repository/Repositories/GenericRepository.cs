using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagement.Repository.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly ECommerceDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(ECommerceDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    // Update ve Delete metotları asenkron olmaz, sadece EF Core'un State'ini değiştirir.
    // Asıl işlem SaveChangesAsync çağrıldığında DB'ye yansır.
    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}