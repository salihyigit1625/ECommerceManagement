namespace ECommerceManagement.Domain.Constants;

public static class AppPermissions
{
    // 1. Kullanıcı ve Yetki Yönetimi (Sadece SuperAdmin)
    public const string ManageRoles = "Users.ManageRoles";
    public const string ManagePermissions = "Users.ManagePermissions";

    // 2. Kategori ve Katalog Yönetimi (Admin)
    public const string ManageCatalog = "Catalog.Manage";
    public const string ReadCatalog = "Catalog.Read";

    // 3. Depo Yönetimi (Admin)
    public const string ManageWarehouses = "Warehouses.Manage";
    public const string ReadWarehouses = "Warehouses.Read";

    // 4. Ürün Yönetimi (Seller)
    public const string ManageProducts = "Products.Manage"; // Sadece kendi ürünlerini ekle/sil
    
    // 5. Sipariş İşlemleri (Customer & Seller)
    public const string CreateOrders = "Orders.Create";     // Müşteri sipariş verebilir
    public const string ReadOrders = "Orders.Read";         // Müşteri kendi, satıcı kendisine gelen siparişi görür
}