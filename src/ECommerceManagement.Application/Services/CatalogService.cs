using AutoMapper;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Services;

public class CatalogService : ICatalogService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public CatalogService(IGenericRepository<Product> productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync()
    {
        // İlişkili tablolardan verileri Include ile çekiyoruz
        var products = await _productRepository.GetAllAsync(
            p => p.Category, 
            p => p.Seller, 
            p => p.Warehouse
        );

        var activeProducts = products.Where(p => p.IsActive && p.Quantity > 0);
        return _mapper.Map<IEnumerable<ProductDto>>(activeProducts);
    }
}