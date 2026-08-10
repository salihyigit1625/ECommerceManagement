using AutoMapper;
using ECommerceManagement.Application.Common;
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

    public async Task<PagedResultDto<ProductDto>> GetActiveProductsPagedAsync(ProductFilterDto filter)
    {
        // 1. Dinamik Filtreleme (Filtering)
        System.Linq.Expressions.Expression<Func<Product, bool>> predicate = p =>
            p.IsActive &&
            p.Quantity > 0 &&
            (string.IsNullOrEmpty(filter.SearchTerm) || p.Name.Contains(filter.SearchTerm) || p.Sku.Contains(filter.SearchTerm)) &&
            (!filter.CategoryId.HasValue || p.CategoryId == filter.CategoryId.Value) &&
            (!filter.MinPrice.HasValue || p.Price >= filter.MinPrice.Value) &&
            (!filter.MaxPrice.HasValue || p.Price <= filter.MaxPrice.Value);

        // 2. Dinamik Sıralama (Sorting)
        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = query =>
        {
            var isDescending = filter.SortOrder.ToLower() == "desc";

            return filter.SortBy?.ToLower() switch
            {
                "price" => isDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "name" => isDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "quantity" => isDescending ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
                _ => query.OrderByDescending(p => p.CreatedAt) // Varsayılan: En yeni eklenenler
            };
        };

        // 3. Veritabanından Sayfalanmış Veriyi Çek (Pagination)
        var (items, totalCount) = await _productRepository.GetPagedAsync(
            predicate,
            orderBy,
            filter.PageNumber,
            filter.PageSize,
            p => p.Category!,
            p => p.Seller!,
            p => p.Warehouse!
        );

        var dtos = _mapper.Map<IEnumerable<ProductDto>>(items);

        return new PagedResultDto<ProductDto>(dtos, totalCount, filter.PageNumber, filter.PageSize);
    }
}