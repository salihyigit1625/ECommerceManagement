using ECommerceManagement.Application.DTOs.Address;
using ECommerceManagement.Application.DTOs.Profiles;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Services;

public class AddressService : IAddressService
{
    private readonly IGenericRepository<Address> _addressRepository;
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddressService(
        IGenericRepository<Address> addressRepository,
        IGenericRepository<Customer> customerRepository,
        IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AddressDto>> GetMyAddressesAsync(int userId)
    {
        var customerId = await GetCustomerIdByUserIdAsync(userId);
        
        var addresses = await _addressRepository.GetWhereAsync(a => a.CustomerId == customerId);

        return addresses
            .Select(a => new AddressDto(
                a.Id,
                a.CustomerId,
                a.Title,
                a.City,
                a.District,
                a.FullAddress,
                a.IsBilling,
                a.IsShipping
            ))
            .ToList();
    }

    public async Task<AddressDto?> GetByIdAsync(int addressId, int userId)
    {
        var customerId = await GetCustomerIdByUserIdAsync(userId);
        var address = await _addressRepository.GetByIdAsync(addressId);

        if (address is null || address.CustomerId != customerId) 
            return null;

        return new AddressDto(
            address.Id,
            address.CustomerId,
            address.Title,
            address.City,
            address.District,
            address.FullAddress,
            address.IsBilling,
            address.IsShipping
        );
    }

    public async Task<AddressDto> CreateAsync(int userId, CreateAddressDto dto)
    {
        var customerId = await GetCustomerIdByUserIdAsync(userId);

        var address = new Address
        {
            CustomerId = customerId,
            Title = dto.Title,
            City = dto.City,
            District = dto.District,
            FullAddress = dto.FullAddress,
            IsBilling = dto.IsBilling,
            IsShipping = dto.IsShipping
        };

        await _addressRepository.AddAsync(address);
        await _unitOfWork.SaveChangesAsync();

        return new AddressDto(
            address.Id,
            address.CustomerId,
            address.Title,
            address.City,
            address.District,
            address.FullAddress,
            address.IsBilling,
            address.IsShipping
        );
    }

    public async Task<AddressDto> UpdateAsync(int userId, UpdateAddressDto dto)
    {
        var customerId = await GetCustomerIdByUserIdAsync(userId);
        var address = await _addressRepository.GetByIdAsync(dto.Id);

        if (address is null || address.CustomerId != customerId)
        {
            throw new KeyNotFoundException("Güncellenmek istenen adres bulunamadı veya bu adrese erişim yetkiniz yok.");
        }

        address.Title = dto.Title;
        address.City = dto.City;
        address.District = dto.District;
        address.FullAddress = dto.FullAddress;
        address.IsBilling = dto.IsBilling;
        address.IsShipping = dto.IsShipping;

        _addressRepository.Update(address);
        await _unitOfWork.SaveChangesAsync();

        return new AddressDto(
            address.Id,
            address.CustomerId,
            address.Title,
            address.City,
            address.District,
            address.FullAddress,
            address.IsBilling,
            address.IsShipping
        );
    }

    public async Task<bool> DeleteAsync(int addressId, int userId)
    {
        var customerId = await GetCustomerIdByUserIdAsync(userId);
        var address = await _addressRepository.GetByIdAsync(addressId);

        if (address is null || address.CustomerId != customerId) 
            return false;

        _addressRepository.Delete(address);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<int> GetCustomerIdByUserIdAsync(int userId)
    {
        var customer = await _customerRepository.GetAsync(c => c.UserId == userId);

        if (customer is null)
        {
            throw new InvalidOperationException("Kullanıcıya ait müşteri profili bulunamadı.");
        }

        return customer.Id;
    }
}