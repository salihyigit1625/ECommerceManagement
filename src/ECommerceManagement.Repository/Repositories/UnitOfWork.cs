using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Repository.Context;

namespace ECommerceManagement.Repository.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ECommerceDbContext _context;

    public UnitOfWork(ECommerceDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}