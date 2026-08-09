namespace ECommerceManagement.Application.DTOs.Profiles;

public class SellerDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}