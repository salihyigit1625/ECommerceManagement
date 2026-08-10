using AutoMapper;
using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.DTOs.Invoices;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ==========================================
        // 1. AUTH & KULLANICI DÖNÜŞÜMLERİ
        // ==========================================
        CreateMap<CustomerRegisterDto, User>();
        CreateMap<CustomerRegisterDto, Customer>();
        CreateMap<SellerRegisterDto, User>();
        CreateMap<SellerRegisterDto, Seller>();

        // ==========================================
        // 2. KATEGORİ & DEPO DÖNÜŞÜMLERİ
        // ==========================================
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>();

        CreateMap<Warehouse, WarehouseDto>();
        CreateMap<CreateWarehouseDto, Warehouse>();

        // ==========================================
        // 3. ÜRÜN (PRODUCT) DÖNÜŞÜMLERİ
        // ==========================================
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>(); 
        
        // Product -> ProductDto (İlişkili tablolardan isim çekme işlemleri)
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Seller != null ? src.Seller.CompanyName : string.Empty))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty));

        // Product -> SellerProductDto
        CreateMap<Product, SellerProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty));

        // ==========================================
        // 4. FATURA (INVOICE) DÖNÜŞÜMLERİ
        // ==========================================
        CreateMap<Invoice, InvoiceDto>();
    }
}