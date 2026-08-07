using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Domain.Entities;

public class Product : BaseEntity
{
    public int SellerId { get; set; }
    public int CategoryId { get; set; }
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }

    public Seller Seller { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductMovement> Movements { get; set; } = new List<ProductMovement>();
}

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }

    public Product Product { get; set; } = null!;
}

public class ProductMovement : BaseEntity
{
    public int ProductId { get; set; }
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public int? ReferenceId { get; set; } // Sipariş ID vb.

    public Product Product { get; set; } = null!;
}