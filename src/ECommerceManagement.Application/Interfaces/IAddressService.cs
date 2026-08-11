using ECommerceManagement.Application.DTOs.Address;

namespace ECommerceManagement.Application.Interfaces;

public interface IAddressService
{
    Task<List<AddressDto>> GetMyAddressesAsync(int userId);
    Task<AddressDto?> GetByIdAsync(int addressId, int userId);
    Task<AddressDto> CreateAsync(int userId, CreateAddressDto dto);
    Task<AddressDto> UpdateAsync(int userId, UpdateAddressDto dto);
    Task<bool> DeleteAsync(int addressId, int userId);
}