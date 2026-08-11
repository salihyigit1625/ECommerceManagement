namespace ECommerceManagement.Application.DTOs.Address;

public record CreateAddressDto(
    string Title,
    string City,
    string District,
    string FullAddress,
    bool IsBilling = false,
    bool IsShipping = false
);

public record UpdateAddressDto(
    int Id,
    string Title,
    string City,
    string District,
    string FullAddress,
    bool IsBilling,
    bool IsShipping
);

public record AddressDto(
    int Id,
    int CustomerId,
    string Title,
    string City,
    string District,
    string FullAddress,
    bool IsBilling,
    bool IsShipping
);