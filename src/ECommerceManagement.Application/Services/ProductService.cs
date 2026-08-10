using AutoMapper;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IGenericRepository<Product> productRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SellerProductDto>> GetProductsBySellerIdAsync(int sellerId)
    {
        var products = await _productRepository.GetAllAsync(
            p => p.Category, 
            p => p.Seller, 
            p => p.Warehouse
        );

        var sellerProducts = products.Where(p => p.SellerId == sellerId);
        return _mapper.Map<IEnumerable<SellerProductDto>>(sellerProducts);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null || !product.IsActive) return null;

        return _mapper.Map<ProductDto>(product);
    }

    public async Task AddAsync(CreateProductDto dto)
    {
        // İş Kuralları
        if (dto.Price <= 0)
            throw new InvalidOperationException("Ürün fiyatı 0'dan büyük olmalıdır.");

        if (dto.Quantity < 0)
            throw new InvalidOperationException("Stok miktarı negatif olamaz.");

        var product = _mapper.Map<Product>(dto);
        product.IsActive = true;

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id);
        if (product == null)
            throw new KeyNotFoundException("Güncellenecek ürün bulunamadı.");

        if (dto.Price <= 0)
            throw new InvalidOperationException("Ürün fiyatı 0'dan büyük olmalıdır.");

        if (dto.Quantity < 0)
            throw new InvalidOperationException("Stok miktarı negatif olamaz.");

        // DTO'daki yeni değerleri var olan Entity'nin üzerine otomatik yazar
        _mapper.Map(dto, product);
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException("Silinecek ürün bulunamadı.");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();
    }
}